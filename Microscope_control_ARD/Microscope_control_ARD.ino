/*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
  Programa para el manejo de un motor PaP para la visualización automatizada en microscópio
  Usando el Driver A4988 de Allegro
  Se usa el motor EM-238, BIPOLAR
  Versión 1. Septiembre de 2016
  Versión 1.ALFA Octubre 2016
                            - Bit de dirección
                                            César Augusto Hernández Espitia
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                         DIAGRAMA DE CONECCIONES DEL DRIVER
 *                                                                                         *
 *                                                                                         *
          (8)      DIRECTION (dir)  ___1|-------|9___ GND          (GND)
          (9)           STEP (paso) ___2|       |10__ VDD           (5V)
          (10)        ~SLEEP (dorm) ___3|       |11__ 1B        To motor
          (11)        ~RESET (rese) ___4|       |12__ 1A        To motor
          (N/C)          MS3        ___5| A4988 |13__ 2A        To motor
          (N/C)          MS2        ___6|       |14__ 2B        To motor
          (N/C)          MS1        ___7|       |15__ GND          (GND)
          (12)       ~ENABLE  (ena) ___8|-------|16__ VMOT         (Vin)*
                                                                          Recomendado 12V >1A
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

#include <EEPROM.h>     //Librería para manejar los datos de la EEPROM

typedef struct          // Estructura de almacenamiento para manejo de pulsos
{
  int pin;
  int tHigh;
  int tLow;
} PULSE;

// Inicializar Pines de control (en este caso para un arduino MEGA)

#define ms1   4   // uStep Control
#define ms2   5
#define ms3   6

#define sht   7   // Pin para control del disparador de la cámara (Uso con disparador remoto)
#define dir   8   // Control de dirección
#define stp   9   // Control de paso
#define slp   10  // ~Sleep
#define rst   11  // ~Reset
#define en    12  // ~Enable
#define led   13  // Led de estado (Parpadeo: en stand by (inicio), encendido: funcionando)
#define start 50  // Botón de control

// Ajustar acá el ancho de los pulsos!!!!!!!!!!


PULSE SH = (PULSE)
{
  sht, 100, 900
};           // Pulso obturador
PULSE ST = (PULSE)
{
  stp, 1, 6
};            // Pulso paso del motor
PULSE LD = (PULSE)
{
  led, 200, 200
};          // Pulso del LED

// Declaración de variables de programa

///////////////
bool startFlag = false;
bool errorFlag = false;
bool endFlag = false;
byte sessionB;
byte sessionRx;
int pos1;
int pos2;
int pos3;
int stepMult = 1;
//////////////

bool cal          = false;  // Bandera de calibración
bool tFlag        = true;
byte sen;
int i;                      // Contadores
int j;
int k;
int startState;             // Variable de estado para el botón de encendido
int rxPos         = 0;      // Posición actual del TrackBar
int lPos          = 0;      // Ultima posición del trackBar
int Pos           = 0;      // Posición para movimiento
int sign          = 1;      // Auxiliar de dirección
int data1         = 0;      // Auxiliar para proceso de guardado
int data2         = 0;      // Auxiliar para proceso de guardado
unsigned long temp     = 0 ;
unsigned long tempRef  = 0;
unsigned long tempDif  = 0;
unsigned long Tmil;
unsigned long LongStep;
int proc = 0;                  // Indicador de proceso
byte rxByte;
String rxData;              // Variable de recepción usada en la lectura de puerto serie
int npas;                   // Número de pasos por imágen
int ccic;                   // Número de ciclos para completar el canal
int tcic;                   // Tiempo entre cada ciclo completo en segundos



void setup()
{
  ReadMem();                                        // Cargar los datos almacenados en la EEPROM

  pinMode(ms1,    OUTPUT);
  pinMode(ms2,    OUTPUT);
  pinMode(ms3,    OUTPUT);

  pinMode(sht,    OUTPUT);                          // Inicializar los pines de la tarjeta
  pinMode(dir,    OUTPUT);
  pinMode(stp,    OUTPUT);
  pinMode(slp,    OUTPUT);
  pinMode(rst,    OUTPUT);
  pinMode(en,     OUTPUT);
  pinMode(led,    OUTPUT);
  pinMode(start,  INPUT_PULLUP);

  PinStart();                                       // Cerciorarse que los pines están en cero

  delay(5);
  digitalWrite(slp, HIGH);
  digitalWrite(rst, HIGH);
  // Microstep all high
  digitalWrite(ms1, LOW);
  digitalWrite(ms2, LOW);
  digitalWrite(ms3, LOW);

  digitalWrite(dir, HIGH);
  Blink(ST, 128);
  digitalWrite(dir, LOW);
  Blink(ST, 128);

  digitalWrite(led, LOW);

  Serial.begin(57600, SERIAL_8N1);
  while (!Serial)
  {
    ;
  }
}

void loop()
{
  if (startFlag) digitalWrite(led, HIGH);
  else digitalWrite(led, LOW);
  if (endFlag)
  {
    Blink (LD, 1);
    Serial.end();
    Blink (LD, 2);
    Serial.begin(57600, SERIAL_8N1);
    endFlag = false;
    while (!Serial)
    {
      ;
    }
  }
}
void serialEvent()
{
  while (Serial.available() > 0)
  {
    delay(1);
    rxByte = Serial.read();
    rxData += char(rxByte);
    if (rxData == ("COMREQU"))
    {
      delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      sessionB = rxByte;
      rxData = "";
      Serial.write("COMSTART");
      Serial.write(rxByte);
      startFlag = true;
    }
    if (rxData == ("COMERROR"))
    {
      rxData = "";
      Serial.write("DISCONNECT");
    }
    if (rxData == ("DISCONNECT"))
    {
      endFlag = true;
      startFlag = false;
      rxData = "";
    }

    if (rxData == ("@"))
    {
      CalibrationHandler();
    }
    if (rxData == ("~"))
    {
      CalibrationHandler();
    }

    if (rxByte == 0x0A)
    {
      //Serial.print(rxData);
      rxData = "";
    }
  }
}

void Blink(PULSE USE, int rep)     // Función para realizar pulsos *Utilizada para el LED, el obturador y el treen de pulsos para la plataforma)
{
  int pin = USE.pin;
  int tHigh = USE.tHigh;
  int tLow = USE.tLow;
  unsigned long timer = millis();
  for (i = 1; i <= rep; i++)
  {

    digitalWrite(pin, LOW);
    while ((millis() - timer) <= (tLow / 2))
    {
      ;
    }
    timer = millis();
    digitalWrite(pin, HIGH);
    while ((millis() - timer) <= (tHigh))
    {
      ;
    }
    timer = millis();
    digitalWrite(pin, LOW);
    while ((millis() - timer) <= (tLow / 2))
    {
      ;
    }
    timer = millis();
  }
}
void PinStart()                                     // Verifica que las salidas digitales esten en cero
{
  for (i = 0; i <= 13; i++)
  {
    digitalWrite(i, LOW);
  }
}

void ReadMem()
{
  npas = ((EEPROM.read(0)) * 100) + EEPROM.read(1); //Carga los valores guardados de configuración
  ccic = ((EEPROM.read(2)) * 100) + EEPROM.read(3);
  tcic = ((EEPROM.read(4)) * 100) + EEPROM.read(5);
  //sen = EEPROM.read(8);
}

void WriteMem(int pos1, int pos2)
{
  rxPos = rxData.toInt();
  data1 = rxPos / 100;
  data2 = rxPos - (data1 * 100);
  rxData = "";
  EEPROM.write(pos1, data1);
  EEPROM.write(pos2, data2);
}

void CalibrationHandler()
{
  //delay(1);
  rxByte = Serial.read();
  rxData += char(rxByte);
  sessionRx = rxByte;
  if (rxByte != sessionB)
  {
    errorFlag = true;
  }
  //delay(1);
  rxByte = Serial.read();
  rxData += char(rxByte);
  switch (rxByte)
  {
    case 'P':
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos1 = int(rxByte);
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos2 = int(rxByte);
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos3 = int(rxByte);
      rxPos = pos1 + (pos2 * 128) + (pos3 * 128 * 128);
      //***********
      Pos = (rxPos - lPos) * (sign);
      if (Pos < 0)
      {
        digitalWrite(dir, LOW);
      }
      else
      {
        digitalWrite(dir, HIGH);
      }
      //delay(1);
      Blink(ST, abs(Pos)*stepMult);

      /*Serial.write("Moving: ");           //Monitor rutine
        Serial.print(Pos);
        Serial.write(" steps\n");
        if (!errorFlag)
        Serial.write("Session OK!!");
        Serial.write("Expected: ");
        Serial.write(sessionB);               //character
        Serial.write(" Received: ");
        Serial.print(sessionRx); */           //number
      lPos    = rxPos;
      //rxPos   = 0;
      //Pos     = 0;
      rxData  = "";
      //sign    = 1;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("MF");
      break;
    case 'I':
      delay(10);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("IF");
      Serial.print(npas);
      Serial.print(",");
      Serial.print(ccic);
      Serial.print(",");
      Serial.print(tcic);
      break;
    case 'O':
      lPos = 0;
      rxPos = 0;
      Pos = 0;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("OF");
      break;
    case 'S':
    case 'Z':
      sen = rxByte;
      if (sen == 'S')
      {
        sign = sign * -1;
      }
      lPos = 0;
      rxPos = 0;
      Pos = 0;
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos1 = int(rxByte);
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos2 = int(rxByte);
      //delay(1);
      rxByte = Serial.read();
      rxData += char(rxByte);
      pos3 = int(rxByte);
      rxPos = pos1 + (pos2 * 128) + (pos3 * 128 * 128);
      Pos =  (rxPos - lPos) * (sign) ;
      if (Pos < 0)
      {
        digitalWrite(dir, LOW);
      }
      else
      {
        digitalWrite(dir, HIGH);
      }
      //delay(1);
      Blink(ST, abs(Pos)*stepMult);
      //delay(1);
      if (sen == 'S')
        sign    = sign * -1;
      lPos    = 0;
      //rxPos   = 0;
      // rxData  = "";
      Pos     = 0;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("SF");
      //Pos = 50;
      break;
    case 'Q':
      //delay(1);
      rxByte = Serial.read();
      ST.tLow = int(rxByte) * 2;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("QF");
      break;
    case 'U':
      stepMult = 1;
      //delay(1);
      digitalWrite(ms1, LOW);
      digitalWrite(ms2, LOW);
      digitalWrite(ms3, LOW);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("UF");
      break;
    case 'W':
      stepMult = 16;
      //delay(1);
      digitalWrite(ms1, HIGH);
      digitalWrite(ms2, HIGH);
      digitalWrite(ms3, HIGH);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("WF");
      break;
    case 'F':
      sign = 1;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("FF");
      break;
    case 'R':
      sign = -1;
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("RF");
      break;
    case 'V':
      rxData = ("");
      rxPos = 0;
      if (Serial.available())                     // Destramar datos
      {
        //delay(1);
        while (Serial.available() > 0)
        {
          data1 = 0;
          data2 = 0;
          rxPos = Serial.read();
          if (isDigit(rxPos))
          {
            rxData += (char)rxPos;
          }
          switch (rxPos)
          {
            case 's':
              WriteMem(0, 1);
              break;
            case 'c':
              WriteMem(2, 3);
              break;
            case 't':
              WriteMem(4, 5);
              break;
          }
        }
      }
      ReadMem();
      //delay(1);
      Serial.print("@");
      Serial.write(sessionRx);
      Serial.print("VF");
      break;
      default:
      

  }
}

