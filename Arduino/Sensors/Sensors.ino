#include <Wire.h>
#include <BH1750.h>
#include "DHT.h"
#include <SoftwareSerial.h>

#define DHTPIN 4
#define DHTTYPE DHT22
#define GPS_BAUDRATE 19200
#define LOOP_DELAY 100 //ms
#define TIMEOUT_PC 1000

//#define NAV_POSLLH_CLASS 0x01
#define NAV_POSLLH_ID 0x02
//#define NAV_TIMEGPS_CLASS 0x01
#define NAV_TIMEGPS_ID 0x20

#define PIN_GPS_CFG 7

//messages from PC:
#define MSG_MEASURE_ALL 0xDA
#define MSG_HELLO 0xE5
//#define DEBUG true

#define PACK_HEAD 0xE1D6

const unsigned char UBX_HEADER[] = { 0xB5, 0x62 };

//variables
BH1750 lightMeter;
DHT dht(DHTPIN, DHTTYPE);
SoftwareSerial gpsSerial(11, 12); //RX, TX

bool gpsCfgMode = false;
bool on = false;

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
  
  gpsSerial.begin(GPS_BAUDRATE);
  
  pinMode(PIN_GPS_CFG, INPUT);
  
  checkSerialMode();
  #ifdef DEBUG
  //Serial.println("Running...");
  #endif
}

byte readCommand()
{
  if(Serial.available() > 0)
  {
    byte b = Serial.read();
    //Serial.println(b, DEC);
    return b;
  }
  else
    return 0;
}

void loop() {
  
  checkSerialMode();
  
  if(gpsCfgMode) //Only if we need configure C94-M8P GPS module
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
  if(millis() % 1000 >= 500)
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
  
  humidity = dht.readHumidity();
  temperature = dht.readTemperature();
  
  if(!isnan(humidity) && !isnan(temperature))
  {
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
    humidity = 0;
    temperature = 0;
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

  Serial.write(buff, len);
  //Serial.flush();
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
