/*
  BLE_Peripheral.ino

  This program uses the ArduinoBLE library to set-up an Arduino Nano 33 BLE 
  as a peripheral device and specifies a service and a characteristic. Depending 
  of the value of the specified characteristic, an on-board LED gets on. 

  The circuit:
  - Arduino Nano 33 BLE. 

  This example code is in the public domain.
*/

#include <ArduinoBLE.h>
#include <Arduino_LSM9DS1.h>

enum {
  COLOR_OFF = -1,
  COLOR_RED    = 0,
  COLOR_GREEN  = 1,
  COLOR_BLUE  = 2,
  COLOR_WHITE  = 3,
  COLOR_PURPLE  = 4,
};

struct Vector3{
  float x,y,z;
};

struct Frame{
  Vector3 acceleration;
  Vector3 rotation;
};

const char* deviceServiceUuid = "19b10000-e8f2-537e-4f6c-d104768a1214";
const char* deviceSendCharacteristicUuid = "19b10001-e8f2-537e-4f6c-d104768a1214";
const char* deviceRecvCharacteristicUuid = "718250e6-7a6e-4b04-8274-bb6c46282199";

BLEService service(deviceServiceUuid); 
BLECharacteristic sendCharacteristic(deviceSendCharacteristicUuid, BLERead | BLENotify, sizeof(Frame));

Frame frame;

void showColor(int color);

void setup() {
  Serial.begin(9600);
  while (!Serial);  
  
  pinMode(LEDR, OUTPUT);
  pinMode(LEDG, OUTPUT);
  pinMode(LEDB, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT);

  digitalWrite(LED_BUILTIN, LOW);

  showColor(COLOR_RED);
  
  if (!BLE.begin()) {
    Serial.println("- Starting Bluetooth® Low Energy module failed!");
    while (1);
  }

  IMU.begin();

  BLE.setLocalName("Arduino Nano 33 BLE (Peripheral)");
  BLE.setAdvertisedService(service);
  service.addCharacteristic(sendCharacteristic);
  BLE.addService(service);
  BLE.advertise();

  Serial.println("Nano 33 BLE (Peripheral Device)");
  Serial.println(" ");
}

void loop() {
  BLEDevice central = BLE.central();
  Serial.println("- Discovering central device...");
  showColor(COLOR_GREEN);
  delay(500);

  if (central) {
    Serial.println("* Connected to central device!");
    Serial.print("* Device MAC address: ");
    Serial.println(central.address());
    Serial.println(" ");
    showColor(COLOR_PURPLE);

    while (central.connected()) {
        bool updated = false;
        if (IMU.accelerationAvailable()) {
          IMU.readAcceleration(frame.acceleration.x, frame.acceleration.y, frame.acceleration.z);
          updated = true;
        }
        if (IMU.gyroscopeAvailable()){
          IMU.readGyroscope(frame.rotation.x, frame.rotation.y, frame.rotation.z);
          updated = true;
        }
        if(updated){
          sendCharacteristic.setValue((const uint8_t*)&frame, sizeof(frame));
        }
        Serial.println(updated);
    }
    
    Serial.println("* Disconnected to central device!");
  }
}

void showColor(int color) {
   switch (color) {
      case COLOR_RED:
        digitalWrite(LEDR, LOW);
        digitalWrite(LEDG, HIGH);
        digitalWrite(LEDB, HIGH);
        break;
      case COLOR_GREEN:
        digitalWrite(LEDR, HIGH);
        digitalWrite(LEDG, LOW);
        digitalWrite(LEDB, HIGH);
        break;
      case COLOR_BLUE:
        digitalWrite(LEDR, HIGH);
        digitalWrite(LEDG, HIGH);
        digitalWrite(LEDB, LOW);
        break;
      case COLOR_PURPLE:
        digitalWrite(LEDR, LOW);
        digitalWrite(LEDG, HIGH);
        digitalWrite(LEDB, LOW);
        break;
      case COLOR_WHITE:
        digitalWrite(LEDR, LOW);
        digitalWrite(LEDG, LOW);
        digitalWrite(LEDB, LOW);
        break;
      default:
        digitalWrite(LEDR, HIGH);
        digitalWrite(LEDG, HIGH);
        digitalWrite(LEDB, HIGH);
        break;
    }      
}