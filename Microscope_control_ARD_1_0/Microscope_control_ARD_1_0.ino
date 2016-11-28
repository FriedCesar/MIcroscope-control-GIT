/*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
  Program for the automated visualization on a microscope using a PaP, PC interface and wireless camera (SONY DSC QX-10)
  Programa para el manejo de un motor PaP para la visualización automatizada en microscópio
  Usando el Driver A4988 de Allegro
  Se usa el motor EM-238, BIPOLAR
  Versión 1. Septiembre de 2016
  Versión 1.ALFA Octubre 2016
                            - Bit de dirección
  Version 1.ALFA November 2016
                            - Complete code modified, the camera has been changed for a SONY DSC QX-10
                            - Translated into english
                            - No shutter on board (Wireless camera)
                            - Modifiable stage moving direction
                            - Variable step method (microsteping 16 steps, or normal step)
                            - Improved communication protocol
                              (Needed, the COM Protocol used before only allowed to calibrate the board, now it has active comunication, in order to permit the interaction PC-Board-Camera))
  Version 1.0 November 2016
                            - Auxiliary motor control added
                            - Changed connection pins
                            - Standarized COM protocol
                            - TODO:
                                    - Temperature control
                                    - Option for IR Control
                                    - Servo-motor

                                            César Augusto Hernández Espitia
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                         MOTOR DRIVER CONNECTION DIAGRAM
                             (Pin + 1 for auxiliar)
 *                                                                                         *
          (46)     DIRECTION (dir)  ___1|-------|9___ GND           (GND)
          (44)          STEP (paso) ___2|       |10__ VDD           (5V)
          (42)        ~SLEEP (dorm) ___3|       |11__ 1B            To motor
          (40)        ~RESET (rese) ___4|       |12__ 1A            To motor
          (48)           MS3        ___5| A4988 |13__ 2A            To motor
          (50)           MS2        ___6|       |14__ 2B            To motor
          (52)           MS1        ___7|       |15__ GND          (GND)
         (n/c)       ~ENABLE  (ena) ___8|-------|16__ VMOT         (Vin)*
                                                                          Recommended 12V >2A
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/

#include <EEPROM.h>         // EEPROM library

typedef struct              // Pulse type structure
{
  int pin;                  // Pin number (Check initialization)
  int tHigh;                // Time for HIGH output
  int tLow;                 // Time for HIGH output
} PULSE;

typedef struct              // Motor type structure
{
  byte sen;                 // Stores direction byte
  int npas;                 // Amount of steps per cycle information
  int ccic;                 // Amount of cycles per image
  int tcic;                 // Time (It doesn't manages timer now, will just save the information related)
  int stepMult;             // Step multiplier, manages uStep (16 pulses = 1 Step)
  int rxPos;                // TrackBar received position
  int lPos;                 // TrackBar last stored position
  int Pos;                  // Steps Processed to move motor
  int sign;                 // Direction Multiplier (Handles direction change)
  int ms1;                  // uStep Control pins
  int ms2;                  //
  int ms3;                  //
  int dir;                  // Direction control pin
  int stp;                  // Step Control pin
  int slp;                  // ~Sleep pin
  int rst;                  // ~Reset pin
  //int en;                  // enable pin (not used)
  PULSE pulse;
} MOTOR;


#define led   13            // State led
PULSE LD = (PULSE)          // LED Pulse structure definition
{
  led, 200, 200
};

// Program variables declaration

bool startFlag = false;     // OnConnection StartFlag is true
bool errorFlag = false;     // OnError errorFlag is true
bool endFlag = false;       // OnDisconnect endFlag is true
byte sessionB;              // Stores session number
byte sessionRx;             // Stores OnCommand Session identifier
byte rxByte;                // Receives and stores OnCommand byte
unsigned char posL;         // Stores 16-byte, high, for position information
unsigned char posH;         // Stores 16-byte, low, for position information
int data1 = 0;              // Save process Data manager
int data2 = 0;              // Save process Data manager
int i;                      // Multipurpose counter
String rxData;              // Command reception storage string

// Calls motor initialization function: (Step multiplier, Received position, Last Position, Current Position, Direction multiplier, uStep1 pin, uStep2 pin, uStep3 pin, direction pin,
//                                       Sleep pin, Reset pin, [Pulse pin, pulse train pin (High status cycle), pulse train pin (Low status cycle)])
MOTOR motorMain;
int MMain[] = {1, 0, 0, 0, 1, 52, 50, 48, 46, 44, 42, 40, 44, 1, 6, 0, 0, 0};
MOTOR motorAux;
int MAux[]  = {1, 0, 0, 0, 1, 53, 51, 49, 47, 45, 43, 41, 45, 1, 6, 0, 0, 0};

void setup()
{
  pinMode(led,    OUTPUT);
  motorMain = initMotor(MMain);
  motorAux  = initMotor(MAux);
  ReadMem();                          // Load EEPROM data

  PinStart();                         // Start every pin in LOW

  delay(5);
  digitalWrite(motorMain.slp, HIGH);  // Activate Stepper Motor Driver
  digitalWrite(motorMain.rst, HIGH);
  digitalWrite(motorMain.dir, HIGH);  // Move motor on startup
  Blink(motorMain.pulse, 50);
  digitalWrite(motorMain.dir, LOW);
  Blink(motorMain.pulse, 50);
  digitalWrite(motorMain.slp, LOW);  // Activate Stepper Motor Driver
  digitalWrite(motorMain.rst, LOW);

  digitalWrite(motorAux.slp, HIGH);   // Activate Stepper Motor Driver (Auxiliar)
  digitalWrite(motorAux.rst, HIGH);
  digitalWrite(motorAux.dir, HIGH);   // Move motor on startup
  Blink(motorAux.pulse, 50);
  digitalWrite(motorAux.dir, LOW);
  Blink(motorAux.pulse, 50);
  digitalWrite(motorAux.slp, LOW);   // Activate Stepper Motor Driver (Auxiliar)
  digitalWrite(motorAux.rst, LOW);

  delay(5);

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
    motorMain = initMotor(MMain);
    motorAux  = initMotor(MAux);
    ReadMem();
    while (!Serial)
    {
      ;
    }
  }
}

//***************************************** Memory related functions *****************************************

void ReadMem()                                      // Reads all data from internal EEPROM
{
  motorMain.npas = ((EEPROM.read(1)) * 256) + EEPROM.read(0);   //Loads on memory values to the motor configuration structure
  motorMain.ccic = ((EEPROM.read(3)) * 256) + EEPROM.read(2);
  motorMain.tcic = ((EEPROM.read(5)) * 256) + EEPROM.read(4);

  motorAux.npas = ((EEPROM.read(11)) * 256) + EEPROM.read(10);    //Loads on memory values to the auxiliary motor configuration structure
  motorAux.ccic = motorMain.ccic;
  motorAux.tcic = motorMain.tcic;
}

void WriteMem(int pos1, int pos2)                 // Writes data into given memory addresses
{
  int dataPos = rxData.toInt();
  data1 = dataPos / 256;
  data2 = dataPos - (data1 * 256);
  rxData = "";
  EEPROM.write(pos1, data1);
  EEPROM.write(pos2, data2);
}

//***************************************** Initialization related functions *****************************************

void PinStart()                                     // Set digital outputs to LOW
{
  for (i = 40; i <= 53; i++)
  {
    digitalWrite(i, LOW);
  }
}

MOTOR initMotor(int initVal[18])                    // Asigns value to motor structures, initialize motor pins
{
  MOTOR myMotor;
  myMotor.stepMult      = initVal[0];
  myMotor.rxPos         = initVal[1];
  myMotor.lPos          = initVal[2];
  myMotor.Pos           = initVal[3];
  myMotor.sign          = initVal[4];
  myMotor.ms1           = initVal[5];
  myMotor.ms2           = initVal[6];
  myMotor.ms3           = initVal[7];
  myMotor.dir           = initVal[8];
  myMotor.stp           = initVal[9];
  myMotor.slp           = initVal[10];
  myMotor.rst           = initVal[11];
  myMotor.pulse.pin     = initVal[12];
  myMotor.pulse.tHigh   = initVal[13];
  myMotor.pulse.tLow    = initVal[14];
  myMotor.npas          = initVal[15];
  myMotor.ccic          = initVal[16];
  myMotor.tcic          = initVal[17];
  pinMode(myMotor.ms1,    OUTPUT);
  pinMode(myMotor.ms2,    OUTPUT);
  pinMode(myMotor.ms3,    OUTPUT);
  pinMode(myMotor.dir,    OUTPUT);
  pinMode(myMotor.stp,    OUTPUT);
  pinMode(myMotor.slp,    OUTPUT);
  pinMode(myMotor.rst,    OUTPUT);
  return myMotor;
}

//***************************************** Program management functions *****************************************

void serialEvent()                                  // Serial event handler, in charge of connection/disconnection and dirtibuting action on motor (Main and auxiliar)
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
      digitalWrite(motorMain.slp, HIGH);                          // Ativate Stepper Motor Driver
      digitalWrite(motorMain.rst, HIGH);
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
      motorMain = initMotor(MMain);
      motorAux  = initMotor(MAux);
      ReadMem();
    }

    if (rxData == ("@"))
    {
      motorMain = ActionHandler(motorMain, '@');
    }
    if (rxData == ("~"))
    {
      motorAux = ActionHandler(motorAux, '~');
    }

    if (rxByte == 0x0A)
    {
      rxData = "";
    }
  }
}


void Blink(PULSE USE, int rep)     // Pulse generator (single cycle is 3ms (value 1 for tHigh and tLow)) *Used for led blink, and motor movement (pulse train)
{
  int pin = USE.pin;
  int tHigh = USE.tHigh;
  int tLow = USE.tLow;
  if (tLow <= 1)
  {
    tLow = 2;
  }
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

MOTOR ActionHandler(MOTOR myMotor, char ID)
{
  unsigned char npasH = motorMain.npas / 256;
  unsigned char npasL = motorMain.npas - (npasH * 256);
  unsigned char ccicH = motorMain.ccic / 256;
  unsigned char ccicL = motorMain.ccic - (ccicH * 256);
  unsigned char tcicH = motorMain.tcic / 256;
  unsigned char tcicL = motorMain.tcic - (tcicH * 256);
  unsigned char npasAuxH = motorAux.npas / 256;
  unsigned char npasAuxL = motorAux.npas - (npasAuxH * 256);

  rxByte = Serial.read();
  rxData += char(rxByte);
  sessionRx = rxByte;
  if (rxByte != sessionB)
  {
    errorFlag = true;
  }
  rxByte = Serial.read();
  rxData += char(rxByte);
  switch (rxByte)
  {
    case 'P':
      rxByte = Serial.read();
      posL = rxByte;
      rxByte = Serial.read();
      posH = rxByte;
      delay(1);
      while (Serial.available() > 0)
      {
        Serial.read();
      }
      myMotor.rxPos = posL + (posH * 256);
      myMotor.Pos = (myMotor.rxPos - myMotor.lPos) * (myMotor.sign);
      if (myMotor.Pos < 0)
      {
        digitalWrite(myMotor.dir, LOW);
      }
      else
      {
        digitalWrite(myMotor.dir, HIGH);
      }
      Blink(myMotor.pulse, abs(myMotor.Pos)*myMotor.stepMult);

      /*Serial.write("Moving: ");           //Monitor rutine
        Serial.print(myMotor.Pos);
        Serial.write(" steps\n");
        if (!errorFlag)
        Serial.write("Session OK!!");
        Serial.write("Expected: ");
        Serial.write(sessionB);               //character
        Serial.write(" Received: ");
        Serial.print(sessionRx); */           //number

      myMotor.lPos    = myMotor.rxPos;
      rxData  = "";
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("MF");
      break;
    case 'I':
      Serial.write(ID);
      Serial.write(sessionRx);
      Serial.write("IF");
      Serial.write(npasL);
      Serial.write(npasH);
      Serial.write(ccicL);
      Serial.write(ccicH);
      Serial.write(tcicL);
      Serial.write(tcicH);
      Serial.write(npasAuxL);
      Serial.write(npasAuxH);
      delay(1);
      break;
    case 'O':
      myMotor.lPos = 0;
      myMotor.rxPos = 0;
      myMotor.Pos = 0;
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("OF");
      break;
    case 'S':
    case 'Z':
      myMotor.sen = rxByte;
      if (myMotor.sen == 'S')
      {
        myMotor.sign = myMotor.sign * -1;
      }
      myMotor.lPos = 0;
      myMotor.rxPos = 0;
      myMotor.Pos = 0;
      rxByte = Serial.read();
      posL = rxByte;
      rxByte = Serial.read();
      posH = rxByte;
      myMotor.rxPos = posL + (posH * 256);
      myMotor.Pos = (myMotor.rxPos - myMotor.lPos) * (myMotor.sign);
      if (myMotor.Pos < 0)
      {
        digitalWrite(myMotor.dir, LOW);
      }
      else
      {
        digitalWrite(myMotor.dir, HIGH);
      }
      Blink(myMotor.pulse, abs(myMotor.Pos)*myMotor.stepMult);
      if (myMotor.sen == 'S')
        myMotor.sign    = myMotor.sign * -1;
      myMotor.lPos    = 0;
      myMotor.Pos     = 0;
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("SF");
      break;
    case 'Q':
      rxByte = Serial.read();
      myMotor.pulse.tLow = int(rxByte);
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("QF");
      break;
    case 'U':
      myMotor.stepMult = 1;
      digitalWrite(myMotor.ms1, LOW);
      digitalWrite(myMotor.ms2, LOW);
      digitalWrite(myMotor.ms3, LOW);
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("UF");
      break;
    case 'W':
      myMotor.stepMult = 16;
      digitalWrite(myMotor.ms1, HIGH);
      digitalWrite(myMotor.ms2, HIGH);
      digitalWrite(myMotor.ms3, HIGH);
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("WF");
      break;
    case 'F':
      myMotor.sign = 1;
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("FF");
      break;
    case 'R':
      myMotor.sign = -1;
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("RF");
      break;
    case 'V':
      for (i = 0; i < 6; i++)
      {
        rxByte = Serial.read();
        EEPROM.write(i, rxByte);
      }
      rxByte = Serial.read();
      EEPROM.write(10, rxByte);
      rxByte = Serial.read();
      EEPROM.write(11, rxByte);
      ReadMem();
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("VF");
      break;
    case 'A':
      digitalWrite(motorAux.slp, HIGH);                          // Ativate auxiliar Stepper Motor Driver
      digitalWrite(motorAux.rst, HIGH);
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("AF");
      break;
    case 'D':
      digitalWrite(motorAux.slp, LOW);                          // Ativate auxiliar Stepper Motor Driver
      digitalWrite(motorAux.rst, LOW);
      Serial.print(ID);
      Serial.write(sessionRx);
      Serial.print("DF");
      break;
    default:
      break;

  }
  return myMotor;
}

