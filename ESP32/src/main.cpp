#include <Arduino.h>

#define KNOP_PIN 13
#define LED_PIN 2
#define SWITCH_PIN 12
#define VIBRATION_PIN 11

void setup() {
  Serial.begin(9600);
  pinMode(KNOP_PIN, INPUT_PULLUP);
  pinMode(LED_PIN, OUTPUT);
  pinMode(SWITCH_PIN, INPUT_PULLUP);
  pinMode(VIBRATION_PIN, OUTPUT);
}

void vibreer() {
  digitalWrite(VIBRATION_PIN, HIGH);
  delay(200);
  digitalWrite(VIBRATION_PIN, LOW);
}

void loop() {
  // Lees commando van Unity EERST
  while (Serial.available() > 0) {
    String commando = Serial.readStringUntil('\n');
    commando.trim();
    if (commando == "VIBRATE") {
      vibreer();
    }
  }

  bool knopIngedrukt = !digitalRead(KNOP_PIN);
  bool schakelaar = !digitalRead(SWITCH_PIN);
  
  digitalWrite(LED_PIN, knopIngedrukt);

  Serial.print("NITRO:");
  Serial.print(knopIngedrukt);
  Serial.print(",SWITCH:");
  Serial.println(schakelaar);

  delay(50);
}