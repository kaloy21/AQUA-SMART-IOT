#include <OneWire.h>
#include <DallasTemperature.h>
#include <WiFi.h>
#include <Firebase_ESP_Client.h>
#include "addons/TokenHelper.h"
#include "addons/RTDBHelper.h"
#include <LiquidCrystal_I2C.h>

#include <WebServer.h>
#include <DNSServer.h>
#include <Preferences.h>

LiquidCrystal_I2C lcd(0x27, 20, 4);



#define API_KEY "AIzaSyAeCTUouX6Jht2gx2oT0nszqcVzhaiB2pI"
#define DATABASE_URL "https://aquasmartiot-de9ce-default-rtdb.firebaseio.com/"  //<databaseName>.firebaseio.com or <databaseName>.<region>.firebasedatabase.app



// ---------------------- TURBIDITY SENSOR ----------------------
int turbidityPin = 35;  // GPIO35 for turbidity sensor
int clearValue = 4095;  // Sensor value in clear water
int dirtyValue = 1000;  // Sensor value in dirty water

// ---------------------- pH SENSOR ----------------------
#define PH_SENSOR 34     // GPIO34 for pH sensor
const float M = -6.0;    // Slope from calibration
const float B = 15.274;  // Intercept from calibration
int buffer_arr[10];
float ph_act;


// ---------------------- TEMPERATURE SENSOR (DS18B20) ----------------------
#define ONE_WIRE_BUS 13  // Data pin of DS18B20
OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

// ---------------------- DISSOLVED OXYGEN SENSOR ----------------------
#define DO_PIN 33    // GPIO33 for DO sensor analog output
#define DO_VREF 3.3  // ESP32 reference voltage
float DO_voltage = 0.0;
float DO_mgL = 0.0;

//  REPLACE THIS with your calibrated air value

float DO_VSaturation = 0.0;

// ---------------------- RELAY PINS ----------------------
#define RELAY1 19           // Circulation Pump 1
#define RELAY2 18           // Circulation Pump 2
#define TEMP_RELAY 17       // Relay for heater
#define TURBIDITY_RELAY 16  // Relay for turbidity control


char pumpStatus[10];
char heaterStatus[10];
char turbidityPumpStatus[10];



FirebaseData fbdo;
FirebaseAuth auth;
FirebaseConfig config;

unsigned long sendDataPrevMillis = 0;
bool signupOK = false;
int ldrData = 0;
float voltage = 0.0;


// ---------------- GLOBALS ----------------
WebServer server(80);
DNSServer dns;
Preferences preferences;

String wifi_ssid="";
String wifi_pass="";



// ---------------- CONFIG PAGE ----------------
const char webpage[] PROGMEM = R"=====(
<!DOCTYPE html>
<html>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1">

<style>

body{
font-family:Arial;
background:#0b3d91;
color:white;
text-align:center;
}

.card{
background:white;
color:black;
padding:20px;
margin:40px auto;
width:300px;
border-radius:12px;
box-shadow:0 4px 10px rgba(0,0,0,0.3);
}

input{
width:90%;
padding:10px;
margin:8px;
border-radius:5px;
border:1px solid gray;
}

button{
background:#0077ff;
color:white;
border:none;
padding:12px 25px;
border-radius:6px;
font-size:16px;
}

</style>

</head>

<body>

<h2>AquaSmart IoT Setup</h2>

<div class="card">

<form action="/save">

<input name="ssid" placeholder="WiFi Name"><br>

<input name="pass" placeholder="WiFi Password" type="password"><br>

<button type="submit">Connect</button>

</form>

</div>

</body>
</html>
)=====";

// ---------------- WEB HANDLERS ----------------

void handleRoot(){
server.send(200,"text/html",webpage);
}

void handleSave(){

wifi_ssid=server.arg("ssid");
wifi_pass=server.arg("pass");

preferences.begin("config",false);

preferences.putString("ssid",wifi_ssid);
preferences.putString("pass",wifi_pass);

preferences.end();

server.send(500,"text/html","Saved! Restarting ESP32...");

delay(2000);
ESP.restart();
}


void handleNotFound(){

server.sendHeader("Location", "http://192.168.4.1", true);
server.send(302, "text/plain", "");

}

// ---------------- PORTAL ----------------

void startPortal(){

WiFi.softAP("AquaSmart Setup");

IPAddress myIP = WiFi.softAPIP();

Serial.println("AP Started");
Serial.println(myIP);

lcd.clear();
lcd.setCursor(0,0);
lcd.print("AquaSmart Setup");

lcd.setCursor(0,1);
lcd.print("Connect WiFi:");

lcd.setCursor(0,2);
lcd.print("AquaSmart Setup");

lcd.setCursor(0,3);
lcd.print("Config Portal");

dns.start(53, "*", myIP);

server.on("/", handleRoot);
server.on("/save", handleSave);
server.onNotFound(handleNotFound);

server.begin();

Serial.println("Config Portal Ready");

while(true){

server.handleClient();
dns.processNextRequest();

}

}

// ---------------- LOAD WIFI ----------------

void loadCredentials(){

preferences.begin("config",true);

wifi_ssid=preferences.getString("ssid","");
wifi_pass=preferences.getString("pass","");

preferences.end();

}


// ---------------------- SETUP ----------------------
void setup() {
  Serial.begin(115200);



  lcd.init();       // Initialize LCD
  lcd.backlight();  // Turn on backlight
  lcd.clear();

  lcd.setCursor(4, 0);
  lcd.print("AQUASMART IOT");
  lcd.setCursor(2, 1);
  lcd.print("Water Monitoring");
  lcd.setCursor(3, 2);
  lcd.print("System Booting");
  delay(2000);

  pinMode(RELAY1, OUTPUT);
  pinMode(RELAY2, OUTPUT);
  pinMode(TEMP_RELAY, OUTPUT);
  pinMode(TURBIDITY_RELAY, OUTPUT);

  // Start with all relays OFF
  digitalWrite(RELAY1, HIGH);
  digitalWrite(RELAY2, HIGH);
  digitalWrite(TEMP_RELAY, LOW);  // Heater OFF at startup
  digitalWrite(TURBIDITY_RELAY, HIGH);

  sensors.begin();

Serial.println("Place DO probe in air-saturated water");
lcd.clear();
lcd.setCursor(0,0);
lcd.print("DO Calibration");
lcd.setCursor(0,1);
lcd.print("Place probe in");
lcd.setCursor(0,2);
lcd.print("air-sat water");
lcd.setCursor(0,3);
lcd.print("Wait 10 sec");

delay(10000);  // time to place probe

DO_VSaturation = analogRead(DO_PIN) * 3.3 / 4095.0;

Serial.print("DO Calibration Voltage: ");
Serial.println(DO_VSaturation);

lcd.clear();
lcd.setCursor(0,0);
lcd.print("DO Calibrated");
lcd.setCursor(0,1);
lcd.print("Voltage:");
lcd.setCursor(0,2);
lcd.print(DO_VSaturation,3);

delay(3000);

loadCredentials();

if(wifi_ssid==""){
startPortal();
}

WiFi.begin(wifi_ssid.c_str(),wifi_pass.c_str());

lcd.clear();
lcd.setCursor(0,0);
lcd.print("Connecting WiFi");

int timeout = 0;

while(WiFi.status()!=WL_CONNECTED){

delay(500);
Serial.print(".");

timeout++;

if(timeout > 30){

Serial.println("WiFi Failed");
startPortal();

}

}

lcd.clear();
lcd.print("WiFi Connected");

config.api_key=API_KEY;
config.database_url=DATABASE_URL;

if(Firebase.signUp(&config,&auth,"","")){
signupOK=true;
}

Firebase.begin(&config,&auth);
Firebase.reconnectWiFi(true);

}

  

// ---------------------- LOOP ----------------------
void loop() {

  // ===== TURBIDITY =====
  int sensorValue = analogRead(turbidityPin);
  int turbidity = map(sensorValue, clearValue, dirtyValue, 0, 100);
  turbidity = constrain(turbidity, 0, 100);

  String turbidityStatus;
  if (turbidity < 30) turbidityStatus = "CLEAR";
  else if (turbidity < 70) turbidityStatus = "CLOUDY";
  else turbidityStatus = "DIRTY";

  // ===== pH =====
  unsigned long avgval = 0;
  int temp;
  for (int i = 0; i < 10; i++) {
    buffer_arr[i] = analogRead(PH_SENSOR);
    delay(30);
  }

  // Sort readings
  for (int i = 0; i < 9; i++)
    for (int j = i + 1; j < 10; j++)
      if (buffer_arr[i] > buffer_arr[j]) {
        temp = buffer_arr[i];
        buffer_arr[i] = buffer_arr[j];
        buffer_arr[j] = temp;
      }

  for (int i = 2; i < 8; i++) avgval += buffer_arr[i];
  float mean_adc = (float)avgval / 6.0;
  float volt = mean_adc * 3.3 / 4096.0;
  ph_act = M * volt + B;

  // ===== TEMPERATURE =====
  sensors.requestTemperatures();
  float tempC = sensors.getTempCByIndex(0);
  float tempF = sensors.toFahrenheit(tempC);

  // ===== DISSOLVED OXYGEN (Using Existing tempC) =====
  int DO_raw = analogRead(DO_PIN);
  DO_voltage = DO_raw * DO_VREF / 4095.0;

  // Temperature-compensated DO saturation formula (freshwater)
  float DO_saturation = 14.6 - (0.4 * tempC) + (0.008 * tempC * tempC);

  // Final DO calculation
  DO_mgL = DO_voltage * DO_saturation / DO_VSaturation;


  // ===== RELAY CONTROL (RELAY1 & RELAY2 BASED ON TEMPERATURE) =====
  if (tempC >= 32.0) {  // Temp ≥ 35°C → turn pumps ON
    digitalWrite(RELAY1, LOW);
    digitalWrite(RELAY2, LOW);
  } else if (tempC <= 30.0) {  // Temp ≤ 32°C → turn pumps OFF
    digitalWrite(RELAY1, HIGH);
    digitalWrite(RELAY2, HIGH);
  }

  // Optional: pH override (turn pumps ON if pH is bad)
  if (ph_act < 6.5 || ph_act > 8.5) {
    digitalWrite(RELAY1, LOW);
    digitalWrite(RELAY2, LOW);
  }

  // ===== HEATER CONTROL =====
  if (tempC <= 26.0) {
    digitalWrite(TEMP_RELAY, HIGH);  // Heater ON
  } else if (tempC > 32.0) {
    digitalWrite(TEMP_RELAY, LOW);  // Heater OFF
  }

  // ===== TURBIDITY RELAY CONTROL =====
  if (turbidity >= 50) {
    digitalWrite(TURBIDITY_RELAY, LOW);  // ON
  } else if (turbidity <= 20) {
    digitalWrite(TURBIDITY_RELAY, HIGH);  // OFF
  }

  if (DO_mgL >= 6) {
    digitalWrite(TURBIDITY_RELAY, LOW);  // ON
  } else if (DO_mgL <= 4) {
    digitalWrite(TURBIDITY_RELAY, HIGH);  // OFF
  }

  // ===== DISPLAY RESULTS =====
  Serial.println("-------------------------------------");
  Serial.print("Turbidity: ");
  Serial.print(sensorValue);
  Serial.print(" | ");
  Serial.print(turbidity);
  Serial.print("% | ");
  Serial.println(turbidityStatus);


 
  Serial.print("pH: ");
  Serial.println(ph_act, 2);

  Serial.print("Temperature: ");
  Serial.print(tempC);
  Serial.print(" °C | ");
  Serial.print(tempF);
  Serial.println(" °F");


  Serial.print("DO: ");
  Serial.print(DO_mgL, 2);
  Serial.println(" mg/L");


  strcpy(pumpStatus, "OFF");  // default
  if (ph_act >= 8.0) {
    strcpy(pumpStatus, "ON");
  } else if (tempC <= 6.0) {
    strcpy(pumpStatus, "OFF");
  }

  // ===== HEATER STATUS =====
  strcpy(heaterStatus, "OFF");  // default
  if (tempC <= 31.0) {
    strcpy(heaterStatus, "ON");
  } else if (tempC >= 32.0) {
    strcpy(heaterStatus, "OFF");
  }

  // ===== TURBIDITY PUMP STATUS =====
  strcpy(turbidityPumpStatus, "OFF");  // default
  if (turbidity >= 50) {
    strcpy(turbidityPumpStatus, "ON");
  } else if (turbidity <= 20) {
    strcpy(turbidityPumpStatus, "OFF");
  }

  //--------------------------------



  Serial.println(pumpStatus);
  Serial.println(heaterStatus);
  Serial.println(turbidityPumpStatus);

  Serial.println("-------------------------------------");
  delay(2000);



  if (Firebase.ready() && signupOK && (millis() - sendDataPrevMillis > 2000 || sendDataPrevMillis == 0)) {
    sendDataPrevMillis = millis();
    
Firebase.RTDB.setInt(&fbdo, "AQUASMART/Turbidity/sensorValue", sensorValue);
Firebase.RTDB.setInt(&fbdo, "AQUASMART/Turbidity/turbidity", turbidity);
Firebase.RTDB.setString(&fbdo, "AQUASMART/Turbidity/turbidityStatus", turbidityStatus);
Firebase.RTDB.setFloat(&fbdo, "AQUASMART/PHVOLTAGE/ph_act", ph_act);
Firebase.RTDB.setFloat(&fbdo, "AQUASMART/TEMPERATURE/CELCIUS", tempC);
Firebase.RTDB.setFloat(&fbdo, "AQUASMART/TEMPERATURE/FAHRENHEIT", tempF);
Firebase.RTDB.setFloat(&fbdo, "AQUASMART/DO/mgL", DO_mgL);
Firebase.RTDB.setString(&fbdo, "AQUASMART/STATUSMESSAGE/CIRCpumpStatus", pumpStatus);
Firebase.RTDB.setString(&fbdo, "AQUASMART/STATUSMESSAGE/heaterStatus", heaterStatus);
Firebase.RTDB.setString(&fbdo, "AQUASMART/STATUSMESSAGE/refillPumpStatus", turbidityPumpStatus);
Serial.println("-------------------------------------");
 delay(500);
 Serial.println("-------------------------------------");
     // Line 1: Temperature
    lcd.setCursor(0, 0);
    lcd.print("Temp:");
    lcd.print(tempC, 1);
    lcd.print("C ");
    lcd.print(tempF, 1);
    lcd.print("F");
     // Line 2: pH
    lcd.setCursor(0, 1);
    lcd.print("pH:");
    lcd.print(ph_act, 2);
    // Line 3: Turbidity
    lcd.setCursor(0, 2);
    lcd.print("                    "); // clear line
    lcd.setCursor(0, 2);
    lcd.print("Turb:");
    lcd.print(turbidity);
    lcd.print("% ");
    lcd.print(turbidityStatus);
    // Line 4: Dissolved Oxygen
    lcd.setCursor(0, 3);
    lcd.print("DO:");
    lcd.print(DO_mgL, 2);
    lcd.print(" mg/L");
    

    delay(3000);
  }
}