#include <Arduino.h>

#define KNOP_PIN 13
#define LED_PIN 2
#define SMS_PIN 12
#define VIBRATION_PIN 11
#define EJECT_PIN 10
#define HONK_PIN 9
#define JUMP_PIN 8
#define LICHT_PIN 7
#define WIPER_PIN 6
#define LASER_PIN 3
#define GAS_PIN 15
#define REM_PIN 14

// Rotary encoder - later toevoegen
// #define CLK_PIN 4
// #define DT_PIN 5
// int stuurHoek = 0;
// int vorigeCLK;

// Non-blocking vibratie
unsigned long vibraatStartTijd = 0;
bool vibraatActief = false;
const int VIBRATIE_DUUR = 200;

// Timing
unsigned long vorigeTijd = 0;
const int INTERVAL = 50;

void setup() {
  Serial.begin(115200); // Sneller dan 9600!
  pinMode(KNOP_PIN, INPUT_PULLUP);
  pinMode(LED_PIN, OUTPUT);
  pinMode(SMS_PIN, INPUT_PULLUP);
  pinMode(VIBRATION_PIN, OUTPUT);
  pinMode(EJECT_PIN, INPUT_PULLUP);
  pinMode(HONK_PIN, INPUT_PULLUP);
  pinMode(JUMP_PIN, INPUT_PULLUP);
  pinMode(LICHT_PIN, INPUT_PULLUP);
  pinMode(WIPER_PIN, INPUT_PULLUP);
  pinMode(LASER_PIN, INPUT_PULLUP);
  pinMode(GAS_PIN, INPUT_PULLUP);
  pinMode(REM_PIN, INPUT_PULLUP);
}

void startVibratie() {
  digitalWrite(VIBRATION_PIN, HIGH);
  vibraatStartTijd = millis();
  vibraatActief = true;
}

void updateVibratie() {
  if (vibraatActief && millis() - vibraatStartTijd >= VIBRATIE_DUUR) {
    digitalWrite(VIBRATION_PIN, LOW);
    vibraatActief = false;
  }
}

void loop() {
  // Lees commando's van Unity
  while (Serial.available() > 0) {
    String commando = Serial.readStringUntil('\n');
    commando.trim();
    if (commando == "VIBRATE") {
      startVibratie();
    }
  }

  // Update vibratie non-blocking
  updateVibratie();

  // Stuur data elke 50ms
  unsigned long nu = millis();
  if (nu - vorigeTijd >= INTERVAL) {
    vorigeTijd = nu;

    bool nitro = !digitalRead(KNOP_PIN);
    bool sms = !digitalRead(SMS_PIN);
    bool eject = !digitalRead(EJECT_PIN);
    bool honk = !digitalRead(HONK_PIN);
    bool jump = !digitalRead(JUMP_PIN);
    bool licht = !digitalRead(LICHT_PIN);
    bool wiper = !digitalRead(WIPER_PIN);
    bool laser = !digitalRead(LASER_PIN);
    bool gas = !digitalRead(GAS_PIN);
    bool rem = !digitalRead(REM_PIN);

    digitalWrite(LED_PIN, nitro);

    Serial.print("NITRO:"); Serial.print(nitro);
    Serial.print(",SMS:"); Serial.print(sms);
    Serial.print(",EJECT:"); Serial.print(eject);
    Serial.print(",HONK:"); Serial.print(honk);
    Serial.print(",JUMP:"); Serial.print(jump);
    Serial.print(",LICHT:"); Serial.print(licht);
    Serial.print(",WIPER:"); Serial.print(wiper);
    Serial.print(",LASER:"); Serial.print(laser);
    Serial.print(",GAS:"); Serial.print(gas);
    Serial.print(",REM:"); Serial.println(rem);
  }
}