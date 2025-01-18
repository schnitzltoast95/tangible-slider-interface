#include <math.h>

const int buttonPin = 2;
const int motorPin = 3;

double questionnaireItems = 0;
double threshold = 0;
double value = 0;
int lastState = LOW;
int currentState;
const char* delimiter = ":";
unsigned long previousMillis;
const long intervall = 20;

double lastPosition = -1;

void setup() {
  Serial.begin(9600);
  pinMode(motorPin, OUTPUT);
  pinMode(buttonPin, INPUT_PULLUP);
  lastPosition = analogRead(A0);
}

// Function to check if the slider value is near the threshold
bool isNearly(double value, double t) {
  double fractpart, intpart;
  fractpart = modf(value, &intpart);
  return (fractpart < t) || (fractpart > 1 - t);
}

void loop() {
    // Check for incoming serial data
    if (Serial.available()) {
        String input = Serial.readStringUntil('\n');
        parseSerialInput(input);
    }

    // between 0 and 1023
    int sensorValue = analogRead(A0);

    if (millis() - previousMillis >= intervall && abs(sensorValue - lastPosition) >= 5) {
        previousMillis = millis();
        Serial.println(sensorValue);
        lastPosition = sensorValue;
    }

    double value = ((double)sensorValue) * (questionnaireItems - 1) / 1024.0;

    // Check if the value is near the threshold and control the motor
    if (questionnaireItems > 0 && isNearly(value, threshold)) {
        digitalWrite(motorPin, HIGH);
    } else {
        digitalWrite(motorPin, LOW);
    }

    // Check if the button state has changed and print when pressed
    int currentState = digitalRead(buttonPin);
    if (lastState == HIGH && currentState == LOW) {
        // reset local state until message is received
        questionnaireItems = 0.0;
        threshold = 0.0;
        // request next questionnaire data
        Serial.print("Pressed:");
        Serial.println(sensorValue);
    }
    lastState = currentState;
}

// Parse input message to update questionnaireItems and threshold
void parseSerialInput(const String& input) {
  char buffer[30];
  input.toCharArray(buffer, sizeof(buffer));
  char* itemsStr = strtok(buffer, delimiter);
  char* thresholdStr = strtok(NULL, delimiter);

  if (itemsStr != NULL && thresholdStr != NULL) {
    questionnaireItems = atof(itemsStr);
    threshold = atof(thresholdStr);
    
    Serial.print("Updated Questionnaire Items: ");
    Serial.println(questionnaireItems);
    Serial.print("Updated Threshold: ");
    Serial.println(threshold);
  } else {
    Serial.print("ERROR AAAAAAAA: ");
    Serial.println(input);
  }
}