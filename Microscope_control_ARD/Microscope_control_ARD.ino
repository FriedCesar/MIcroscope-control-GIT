/*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
  Program for the automated visualization on a microscope using a PaP, PC interface and wireless camera (SONY DSC QX-10)
  Programa para el manejo de un motor PaP para la visualización automatizada en microscópio
  Usando el Driver A4988 de Allegro
  Se usa el motor EM-238, BIPOLAR
  Versión 1. Septiembre de 2016
  Versión 1.ALFA Octubre 2016
                            - Bit de dirección
  Versión 1.ALFA Noviembre 2016
                            - Complete code modified, the camera has been changed for a SONY DSC QX-10
                            - Translated into english
                            - No shutter on board (Wireless camera)
                            - Modifiable stage moving direction
                            - Variable step method (microsteping 16 steps, or normal step)
                            - Improved communication protocol
                              (Needed, the COM Protocol used before only allowed to calibrate the board, now it has active comunication, in order to permit the interaction PC-Board-Camera))
                            - TODO:
                                    - Improve and standarize the COM Protocol (Single structure for every command)
                                    - Standarize data format (ASCII encoding for every value)
                                    - Temperature control
                                    - Option for IR Control, Temp ON-OFF,Servo-motor

                                            César Augusto Hernández Espitia
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                         MOTOR DRIVER CONNECTION DIAGRAM
 *                                                                                         *
 *                                                                                         *
          (8)      DIRECTION (dir)  ___1|-------|9___ GND          (GND)
          (9)           STEP (paso) ___2|       |10__ VDD           (5V)
          (10)        ~SLEEP (dorm) ___3|       |11__ 1B        To motor
          (11)        ~RESET (rese) ___4|       |12__ 1A        To motor
          (06)           MS3        ___5| A4988 |13__ 2A        To motor
          (05)           MS2        ___6|       |14__ 2B        To motor
          (04)           MS1        ___7|       |15__ GND          (GND)
          (12)       ~ENABLE  (ena) ___8|-------|16__ VMOT         (Vin)*
                                                                          Recommended 12V >1A
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

#include <EEPROM.h>     // EEPROM lybrary

typedef struct          // Pulse type structure
{
  int pin;              // Pin number (Check initialization)
  int tHigh;            // Time for HIGH output
  int tLow;             // Time for HIGH output
} PULSE;

// Control pininitialization (Current ARDUINO MEGA)

#define ms1   4   // uStep Control
#define ms2   5
#define ms3   6
#define sht   7   // IR Shutter controlPin    (Not used,left in code for future changes)
#define dir   8   // Direction control
#define stp   9   // Step Control
#define slp   10  // ~Sleep
#define rst   11  // ~Reset
#define en    12  // ~Enable
#define led   13  // State led
#define start 50  // Control switch           (Not used,left in code for future changes)


PULSE ST = (PULSE)              // Motor pulse structure definition
{
  stp, 1, 6
};
PULSE LD = (PULSE)              // LED Pulse structure definition
{
  led, 200, 200
};
//PULSE SH = (PULSE)              // Shutter pulse structure
//{
//  sht, 100, 900
//};                                            //(Not declared,left in code for future changes)

// Declaración de variables de programa

//////////////////////////////////////////////////// Old variables (For reference) //////////////////////////////////////
//int pos1;
//int pos2;
//int pos3;
//bool cal          = false;  // Bandera de calibración
//bool tFlag        = true;
//int j;
//int k;
//int startState;             // Variable de estado para el botón de encendido
//unsigned long temp     = 0 ;
//unsigned long tempRef  = 0;
//unsigned long tempDif  = 0;
//unsigned long Tmil;
//unsigned long LongStep;
//int proc = 0;                  // Indicador de proceso

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool startFlag = false;     // OnConnection StartFlag is true
bool errorFlag = false;     // OnError errorFlag is true
bool endFlag = false;       // OnDisconnect endFlag is true
byte sessionB;              // Stores session number
byte sessionRx;             // Stores OnCommand Session identifier
byte sen;                   // Stores direction byte
byte rxByte;                // Receives and stores OnCommand byte
unsigned char posL;         // Stores 16-byte, high, for position information
unsigned char posH;         // Stores 16-byte, low, for position information
int stepMult = 1;           // Step multiplier, manages uStep (16 pulses = 1 Step)
int rxPos         = 0;      // TrackBar received position
int lPos          = 0;      // TrackBar last stored position
int Pos           = 0;      // Steps Processed to move motor
int sign          = 1;      // Direction Multiplier (Handles direction change)
int data1         = 0;      // Save process Data manager
int data2         = 0;      // Save process Data manager
int npas;                   // Amount of steps per cycle information
int ccic;                   // Amount of cycles per image
int tcic;                   // Time (It doesn't manages timer now, will just save the information related)
int i;                      // Multipurpose counter
String rxData;              // Command reception storage string
//////////////



void setup()
{
  ReadMem();                                        // Load EEPROM data

  pinMode(ms1,    OUTPUT);
  pinMode(ms2,    OUTPUT);
  pinMode(ms3,    OUTPUT);

  pinMode(sht,    OUTPUT);                          // Initialize board pins
  pinMode(dir,    OUTPUT);
  pinMode(stp,    OUTPUT);
  pinMode(slp,    OUTPUT);
  pinMode(rst,    OUTPUT);
  pinMode(en,     OUTPUT);
  pinMode(led,    OUTPUT);
  pinMode(start,  INPUT_PULLUP);

  PinStart();                                       // Start every pin in LOW

  delay(5);
  digitalWrite(slp, HIGH);                          // Ativate Stepper Motor Driver
  digitalWrite(rst, HIGH);

  //  digitalWrite(ms1, LOW);                           // Assure NO Microstep (Microstep: all HIGH)
  //  digitalWrite(ms2, LOW);
  //  digitalWrite(ms3, LOW);                           // Not needed (On LOW by PinStart)

  digitalWrite(dir, HIGH);                          // Move motor on startup
  Blink(ST, 128);
  digitalWrite(dir, LOW);
  Blink(ST, 128);

  delay(5);
  digitalWrite(slp, LOW);                          // Deativate Stepper Motor Driver
  digitalWrite(rst, LOW);

  digitalWrite(led, LOW);

  Serial.begin(57600, SERIAL_8N1);                  // Start communication (Hearing)
  while (!Serial)
  {
    ;
  }
}

void loop()                                         // Check connection status and vizualize state on board (Connected: LED ON, Disconnected: LED OFF, OnDisconnect: LED Blinks)
{
  if (startFlag) digitalWrite(led, HIGH);
  else digitalWrite(led, LOW);
  if (endFlag)
  {
    PinStart();
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
      delay(5);
      digitalWrite(slp, HIGH);                          // Ativate Stepper Motor Driver
      digitalWrite(rst, HIGH);
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
      //delay(2);
      rxByte = Serial.read();
      posL = rxByte;
      //delay(2);
      rxByte = Serial.read();
      posH = rxByte;
      //delay(2);
      rxPos = posL + (posH * 256);
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
      //delay(1);
      rxByte = Serial.read();
      posL = rxByte;
      //delay(1);
      rxByte = Serial.read();
      posH = rxByte;
      //delay(1);
      rxPos = posL + (posH * 256);
      //***********
      Pos = (rxPos - lPos) * (sign);
      /*      rxByte = Serial.read();
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
            rxPos = pos1 + (pos2 * 128) + (pos3 * 128 * 128);*/
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
      break;

  }
}

