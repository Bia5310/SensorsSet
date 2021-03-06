/*
============================
<  Firmware Version 1.05   >
============================
*/

#include <Wire.h>
#include <BH1750.h>
#include "DHT.h"
#include <SoftwareSerial.h>

#define DHTPIN 4
#define DHTTYPE DHT22
#define GPS_BAUDRATE 19200
#define FILTER_BAUDRATE 115200
#define LOOP_DELAY 100 //ms
#define TIMEOUT_PC 1000

//Master PC or AOF
#define MASTER_PC 1
#define MASTER_FILTER 2

//#define NAV_POSLLH_CLASS 0x01
#define NAV_POSLLH_ID 0x02
//#define NAV_TIMEGPS_CLASS 0x01
#define NAV_TIMEGPS_ID 0x20

#define PIN_GPS_CFG 7
#define PIN_MASTER_CFG 8

//messages from PC:
#define MSG_MEASURE_ALL 0xDA
#define MSG_HELLO 0xE5
//#define DEBUG true //comment this to off DEBUF

#define PACK_HEAD 0xE1D6

const unsigned char UBX_HEADER[] = { 0xB5, 0x62 };

//variables
BH1750 lightMeter;
DHT dht(DHTPIN, DHTTYPE);
SoftwareSerial gpsSerial(11, 12); //RX, TX

bool gpsCfgMode = false;
bool on = false;

int master = MASTER_PC; //If master == MASTER_PC, communication will via Serial, if MASTER_FILTER => filterSerial

float humidity = 0;
float temperature = 0;

uint16_t timer1 = 0;

char report[80];

struct NAV_POSLLH {
  unsigned long iTOW;
  long lon;
  long lat;
  long height;
  long hMSL;
  unsigned long hAcc;
  unsigned long vAcc;
};

struct NAV_TIMEGPS {
  unsigned long iTOW; //U4 ms
  long fTOW; //I4 ns
  short week; //I2
  char leapS; //I1 s
  byte valid; //X1 [- - - - - 2 1 0]   (2-leapsValid, 1-weekValid, 0-towValid)
  unsigned long tAcc; //U4 ns
};

struct PACK_INFO{
  unsigned char cls;
  unsigned char id;
  unsigned short len;
};

struct DATA_PACKAGE {
  uint16_t head;
  uint16_t packsize;
  uint16_t light;
  long humidity; //*1e-5
  long temperature; //*1e-5
  long longitude; //1e-7
  long latitude; //1e-7
  unsigned long iTOW; //time of week
  short week;
};

PACK_INFO info;
NAV_POSLLH posllh;
NAV_TIMEGPS timegps;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(GPS_BAUDRATE);
  Serial.setTimeout(TIMEOUT_PC);
  
  Wire.begin();
  lightMeter.begin();
  dht.begin();
  
  pinMode(PIN_GPS_CFG, INPUT);
  pinMode(PIN_MASTER_CFG, INPUT);

  checkMaster();
  checkSerialMode();
  #ifdef DEBUG
  if(master == MASTER_PC)
    Serial.println("Running...(Master - PC)");
  if(master == MASTER_FILTER)
    Serial.println("Running...(Master - AOF)");
  #endif

  gpsSerial.begin(GPS_BAUDRATE);
  if(master == MASTER_PC)
  {
    filterSerial.begin(FILTER_BAUDRATE);
    filterSerial.setTimeout(TIMEOUT_PC);
  }
}

void checkMaster()
{
  int r = digitalRead(PIN_MASTER_CFG);
  if( r == LOW)
  {
    master = MASTER_PC;
  }
  else if(r == HIGH)
  {
    master = MASTER_FILTER;
  }
  else
  {
    master = MASTER_PC;
  }
}

byte readCommand()
{
  if(SerialAvailable() > 0)
  {
    byte b = SerialRead();
    return b;
  }
  else
    return 0;
}

void loop() {
  
  checkSerialMode();
  
  if(gpsCfgMode && master == MASTER_PC) //Only if we need configure C94-M8P GPS module
  {
    if (gpsSerial.available() > 0)
      Serial.write(gpsSerial.read());
    if(Serial.available() > 0)
      gpsSerial.write(Serial.read());
    return;
  }
  
  byte command = readCommand();
  if(command == MSG_MEASURE_ALL)
  {
    #ifdef DEBUG
      Serial.println("Measuring...");
    #endif
    CollectAndSendData();
  }
  if(command == MSG_HELLO)
  {
    Serial.write(MSG_HELLO-1);
    Serial.flush();
  }
  
  processGPS(2000);
  
  //Indicate diode, that all right
  if(millis() % 1000 >= 500)
    digitalWrite(LED_BUILTIN, LOW);
  else
    digitalWrite(LED_BUILTIN, HIGH);
  //on = !on;

  #ifdef DEBUG
  if(millis() % 5000 >= 1500)
    CollectAndSendData();
  #endif
  
  /*delay(LOOP_DELAY);
  timer1 += LOOP_DELAY;*/
}

void CollectAndSendData()
{
  //Get NAV-POSLLH data
  /*if ( processGPS(2000) ) 
  {
    // do something with GPS data
  }*/
  
  uint16_t lux = lightMeter.readLightLevel();
  
  float hum = dht.readHumidity();
  float temp = dht.readTemperature();
  
  if(!isnan(hum) && !isnan(temp))
  {
    humidity = hum;
    temperature = temp;
    #ifdef DEBUG
    Serial.print("Humidity: ");
    Serial.print(humidity);
    Serial.print(" %\t");
    Serial.print("Temperature: ");
    Serial.print(temperature);
    Serial.println(" *C");
    #endif
  }
  else
  {
    //humidity = 0;
    //temperature = 0;
    #ifdef DEBUG
    Serial.println("DHT read error");
    #endif
  }
  #ifndef DEBUG

  //Packing Data
  DATA_PACKAGE package 
  {
    PACK_HEAD,
    0,
    lux,
    (long)(humidity*1e5),
    (long)(temperature*1e5),
    posllh.lon,
    posllh.lat,
    timegps.iTOW,
    timegps.week
  };
  SendPackage(package);
  
  #endif

  #ifdef DEBUG
  //Light level
  Serial.println(report);
  Serial.print("Light: ");
  Serial.print(lux);
  Serial.println(" lx");
  //GPS position
  Serial.print("Longitude: ");
  Serial.print(posllh.lon*1e-7);
  Serial.println("*");
  Serial.print("Latitude: ");
  Serial.print(posllh.lat*1e-7);
  Serial.println("*");
  //GPS Time
  Serial.print("TimeOfWeek: ");
  Serial.print(timegps.iTOW);
  Serial.println("s");
  Serial.print("Weeks");
  Serial.println(timegps.week);
  
  Serial.println("--------------------------");

  /*if(master == MASTER_FILTER)
  {
    char chs[] = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26 };
    SerialWrite(chs, 26);
  }*/
  #endif
}

void SendPackage(DATA_PACKAGE pack)
{
  int len = (int)sizeof(DATA_PACKAGE);
  char buff[len];
  pack.packsize = len;
  
  unsigned char* pckptr = (unsigned char*)(&pack);
  for(int i = 0; i < len; i++)
  {
    buff[i] = pckptr[i];
  }

  #ifdef DEBUG
    Serial.print("PACKSIZE: ");
    Serial.println(len, DEC);
  #endif
  
  SerialWrite(buff, len);
  SerialFlush();
}

void checkSerialMode()
{
  gpsCfgMode = digitalRead(PIN_GPS_CFG) == HIGH;
}

void calcChecksum(unsigned char* CK, unsigned char* info, int infoSize, unsigned char* ptr, int payloadSize) {
  memset(CK, 0, 2);
  for (int i = 0; i < infoSize; i++) {
    CK[0] += info[i];
    CK[1] += CK[0];
  }
  for (int i = 0; i < payloadSize; i++) {
    CK[0] += ptr[i];
    CK[1] += CK[0];
  }
}

bool processGPS(unsigned long timeout) {
  static int fpos = 0;
  static unsigned char checksum[2];
  static int payloadSize = 10;//специально чуть больше, чем надо;
  int infoSize = sizeof(PACK_INFO);
  //unsigned long timer = millis();
  static unsigned char* ptr;
  
  while ( true ) {

    if(gpsSerial.available() <= 0)
    {
      break; //no data
    }
 
    /*if(millis() - timer > timeout)
      return false;*/
    
    byte c = gpsSerial.read();
    if ( fpos < 2 ) {
      if ( c == UBX_HEADER[fpos] )
        fpos++;
      else
        fpos = 0;
    }
    else {
      if ( (fpos-2) < infoSize) //fill info struct
      {
        ((unsigned char*)(&info))[fpos-2] = c;
        if( fpos - 2 == infoSize-1 )
        {
          if(info.id == NAV_POSLLH_ID)
            ptr = (unsigned char*) &posllh;
          else if(info.id == NAV_TIMEGPS_ID)
            ptr = (unsigned char*) &timegps;
          else
            fpos = 0;
          
          payloadSize = info.len;
        }
      }
      
      if ( fpos >= (infoSize+2) && fpos < (payloadSize + infoSize + 2))
        ptr[fpos-2-infoSize] = c;
      
      fpos++;
      
      if ( fpos == (infoSize+payloadSize+2) ) {
        calcChecksum(checksum, (unsigned char*)&info, infoSize, ptr, payloadSize);
      }
      else if ( fpos == (infoSize+payloadSize+3) ) {
        if ( c != checksum[0] )
          fpos = 0;
      }
      else if ( fpos == (infoSize+payloadSize+4) ) {
        fpos = 0;
        if ( c == checksum[1] ) {
          return true;
        }
      }
      else if ( fpos > (infoSize+payloadSize+4) ) {
        fpos = 0;
      }
    }
  }
  return false;
}

/*
int SerialAvailable()
{
  if(master == MASTER_PC)
  {
    return Serial.available();
  }
  else if(master == MASTER_FILTER)
  {
    return filterSerial.available();
  }
}

byte SerialRead()
{
  if(master == MASTER_PC)
  {
    return Serial.read();
  }
  else if(master == MASTER_FILTER)
  {
    return filterSerial.read();
  }
}

void SerialWrite(char* buff, int len)
{
  if(master == MASTER_PC)
  {
    Serial.write(buff, len);
  }
  else if(master == MASTER_FILTER)
  {
    filterSerial.write(buff, len);
  }
}

void SerialWrite(byte b)
{
  if(master == MASTER_PC)
  {
    Serial.write(b);
  }
  else if(master == MASTER_FILTER)
  {
    filterSerial.write(b);
  }
}

void SerialFlush()
{
  if(master == MASTER_PC)
  {
    Serial.flush();
  }
  else if(master == MASTER_FILTER)
  {
    filterSerial.flush();
  }
}*/
