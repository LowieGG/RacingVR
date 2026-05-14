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

void setup() {
  Serial.begin(9600);
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

  // Rotary encoder - later toevoegen
  // pinMode(CLK_PIN, INPUT);
  // pinMode(DT_PIN, INPUT);
  // vorigeCLK = digitalRead(CLK_PIN);
}

void vibreer() {
  digitalWrite(VIBRATION_PIN, HIGH);
  delay(200);
  digitalWrite(VIBRATION_PIN, LOW);
}

// Rotary encoder - later toevoegen
// void leesRotary() {
//   int huidigeCLK = digitalRead(CLK_PIN);
//   if (huidigeCLK != vorigeCLK) {
//     if (digitalRead(DT_PIN) != huidigeCLK) {
//       stuurHoek++;
//     } else {
//       stuurHoek--;
//     }
//     stuurHoek = constrain(stuurHoek, -100, 100);
//   }
//   vorigeCLK = huidigeCLK;
// }

void loop() {
  while (Serial.available() > 0) {
    String commando = Serial.readStringUntil('\n');
    commando.trim();
    if (commando == "VIBRATE") {
      vibreer();
    }
  }

  // leesRotary(); // Rotary encoder - later toevoegen

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

  Serial.print("NITRO:");
  Serial.print(nitro);
  Serial.print(",SMS:");
  Serial.print(sms);
  Serial.print(",EJECT:");
  Serial.print(eject);
  Serial.print(",HONK:");
  Serial.print(honk);
  Serial.print(",JUMP:");
  Serial.print(jump);
  Serial.print(",LICHT:");
  Serial.print(licht);
  Serial.print(",WIPER:");
  Serial.print(wiper);
  Serial.print(",LASER:");
  Serial.print(laser);
  Serial.print(",GAS:");
  Serial.print(gas);
  Serial.print(",REM:");
  Serial.println(rem);

  delay(50);
}