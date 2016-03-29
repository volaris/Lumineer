#include <RFM69.h>
#include <SPI.h>
#include <Adafruit_NeoPixel.h>
#include <LowPower.h>

#define NETWORKID     101
#define GATEWAYID     1
#define FREQUENCY     RF69_868MHZ //Match this with the version of your Moteino! (others: RF69_433MHZ, RF69_868MHZ)
#define KEY           "LumineerLanterns" //has to be same 16 characters/bytes on all nodes, not more not less!
#define LED           9
#define SERIAL_BAUD   9600
#define NUM_LANTERNS  20
#define LED_PIN       7
#define NUM_PIXELS    2
#define PACKET_SIZE   6
#define NUM_RAINBOW   7
#define NUM_RETRIES   2
#define RETRY_DELAY   10
#define CYCLE_SLACK   1000
#define FADE_TIME     4000
#define FPS           30

#define LOW_POWER     1

int id = 0;
long lastSequenceNumber = -1;
RFM69 radio;

// Parameter 1 = number of pixels in strip
// Parameter 2 = Arduino pin number (most are valid)
// Parameter 3 = pixel type flags, add together as needed:
//   NEO_KHZ800  800 KHz bitstream (most NeoPixel products w/WS2812 LEDs)
//   NEO_KHZ400  400 KHz (classic 'v1' (not v2) FLORA pixels, WS2811 drivers)
//   NEO_GRB     Pixels are wired for GRB bitstream (most NeoPixel products)
//   NEO_RGB     Pixels are wired for RGB bitstream (v1 FLORA pixels, not v2)
Adafruit_NeoPixel strip = Adafruit_NeoPixel(NUM_PIXELS, LED_PIN, NEO_GRB + NEO_KHZ800);

uint32_t rainbow7[7];
byte goals[NUM_LANTERNS];
uint32_t colors[2];

void setup() 
{
  // put your setup code here, to run once:
  Serial.begin(SERIAL_BAUD);
  while(!Serial)
  {
    ;
  }
  pinMode(A0, INPUT_PULLUP);
  pinMode(A1, INPUT_PULLUP);
  pinMode(A2, INPUT_PULLUP);
  pinMode(A3, INPUT_PULLUP);
  pinMode(A4, INPUT_PULLUP);
  
  id |= digitalRead(A4) == LOW ? 0x1 << 4 : 0;
  id |= digitalRead(A3) == LOW ? 0x1 << 3 : 0;
  id |= digitalRead(A2) == LOW ? 0x1 << 2 : 0;
  id |= digitalRead(A1) == LOW ? 0x1 << 1 : 0;
  id |= digitalRead(A0) == LOW ? 0x1 : 0;
  
  Serial.print("id: ");
  Serial.println(id, HEX);
  
  rainbow7[0] = strip.Color(0xFF, 0, 0);
  rainbow7[1] = strip.Color(0xFF, 0xA5, 0);
  rainbow7[2] = strip.Color(0xFF, 0xFF, 0);
  rainbow7[3] = strip.Color(0, 0xFF, 0);
  rainbow7[4] = strip.Color(0, 0, 0xFF);
  rainbow7[5] = strip.Color(0x4B, 0, 0x82);
  rainbow7[6] = strip.Color(0xFF, 0, 0xFF);
  
  colors[0] = 0;
  colors[1] = 0;
  
  for(int i = 0; i < NUM_LANTERNS; i++)
  {
    goals[i] = i % NUM_RAINBOW;
  }
  
  strip.begin();
  strip.show(); // Initialize all pixels to 'off'
  
  radio.initialize(FREQUENCY,id,NETWORKID);
}

void loop() 
{
  // put your main code here, to run repeatedly:
  uint32_t color;
  boolean newTarget = false;
  
  if(id == 0)
  {
    // this is the master
    // wait for everyone to catch up
    delay(CYCLE_SLACK);
    
    // update goasl
    for(int i = 0; i < NUM_LANTERNS; i++)
    {
      goals[i] = (goals[i] + 1) % NUM_RAINBOW;
    }
    
    // start building packet
    byte packet[6];
    packet[0] = ((byte*)&lastSequenceNumber)[0];
    packet[1] = ((byte*)&lastSequenceNumber)[1];
    
    // send more than once to account for packet loss
    // the receiver will handle multiple copies just fine
    for(int retry = 0; retry < NUM_RETRIES; retry++)
    {
      // build packet & send
      for(int i = 0; i < NUM_LANTERNS; i++)
      {
        uint32_t goalColor = rainbow7[goals[i]];
        if(i == 0)
        {
          color = goalColor;
          newTarget = true;
        }
        else
        {
          packet[2] = ((byte*)&goalColor)[0];
          packet[3] = ((byte*)&goalColor)[1];
          packet[4] = ((byte*)&goalColor)[2];
          packet[5] = ((byte*)&goalColor)[3];
          
          radio.send((byte)i, packet, 6);
        }
      }
      delay(RETRY_DELAY);
    }
    lastSequenceNumber++;
    
    radio.sleep();
                      
    Blink(LED,3);
    #ifdef LOW_POWER
      LowPower.powerDown(SLEEP_500MS, ADC_OFF, BOD_OFF);  
    #else
      delay(500);
    #endif
  }
  else
  {
    // this is a slave
    int sequenceNumber = -1;
    
    // TODO: receive
    //check for any received packets
    if (radio.receiveDone())
    {
      int remainingLength = radio.DATALEN;
      int currentOffset = 0;
      
      Serial.print("id: ");
      Serial.println(id, HEX);
      Serial.print("dataLen: ");
      Serial.println(remainingLength, HEX);
      
      // we need to drain the whole buffer
      while(remainingLength >= PACKET_SIZE)
      {  
        byte seqData[2];
        seqData[0] = radio.DATA[currentOffset];
        seqData[1] = radio.DATA[currentOffset + 1];
        sequenceNumber = ((uint16_t*)seqData)[0];
        
        if (sequenceNumber > lastSequenceNumber)
        {
          byte colorData[4];
          colorData[0] = radio.DATA[currentOffset + 2];
          colorData[1] = radio.DATA[currentOffset + 3];
          colorData[2] = radio.DATA[currentOffset + 4];
          colorData[3] = radio.DATA[currentOffset + 5];
          color = ((uint32_t*)colorData)[0];
          
          newTarget = true;          
          lastSequenceNumber = sequenceNumber;
        }
 
        remainingLength -= PACKET_SIZE;
        currentOffset += PACKET_SIZE;
      }
    
      if(newTarget)
      {
        radio.sleep();
  
        Blink(LED,3);
        #ifdef LOW_POWER
          LowPower.powerDown(SLEEP_500MS, ADC_OFF, BOD_OFF);  
        #else
          delay(500);
        #endif
      }
    }
  }
    
  if(newTarget)
  {
    // calculate step size, count, & duration
    int duration = 1000 / FPS;
    int stepCount = FADE_TIME / duration;
    float stepSize = 1.0f / (float)stepCount;
    
    // loop through fade steps
    for(int i = 0; i < stepCount; i++)
    {
      float stepVal = stepSize * i;
      uint32_t color0 = GetGradientColor(colors[0], color, stepVal, true); 
      uint32_t color1 = GetGradientColor(colors[1], colors[0], stepVal, true);
      strip.setPixelColor(0, color0);
      strip.setPixelColor(1, color1);
      strip.show();
      
      #ifdef LOW_POWER
        //todo: better calculation, for now we know it is 33ms
        LowPower.powerDown(SLEEP_30MS, ADC_OFF, BOD_OFF);  
        delay(duration-30);
      #else
        delay(duration);
      #endif
    }
    
    colors[1] = colors[0];
    colors[0] = color;
  }
  
  // wake back up  
  radio.receiveDone();
}

void Blink(byte PIN, int DELAY_MS)
{
  pinMode(PIN, OUTPUT);
  digitalWrite(PIN,HIGH);
  delay(DELAY_MS);
  digitalWrite(PIN,LOW);
}

// use n as a percent to determine the color between start and goal
// n should be in the range (0,1), if it isn't wraparound can occur,
// preventOverlow will cap the value
uint32_t GetGradientColor(uint32_t start, uint32_t goal, float n, boolean preventOverflow)
{
  uint8_t redDelta, goalRed, startRed, redDiff, red;
  uint8_t greenDelta, goalGreen, startGreen, greenDiff, green;
  uint8_t blueDelta, goalBlue, startBlue, blueDiff, blue;

  goalRed = (0x00FF0000 & goal) >> 16;
  goalGreen = (0x0000FF00 & goal) >> 8;
  goalBlue = (0x000000FF & goal);
  
  startRed = (0x00FF0000 & start) >> 16;
  startGreen = (0x0000FF00 & start) >> 8;
  startBlue = (0x000000FF & start);

  redDelta = abs(goalRed - startRed);
  blueDelta = abs(goalBlue - startBlue);
  greenDelta = abs(goalGreen - startGreen);

  if (preventOverflow)
  {
    n = min(1.0f, max(0.0f, n));
  }

  redDiff = (byte)((redDelta * n));
  blueDiff = (byte)((blueDelta * n));
  greenDiff = (byte)((greenDelta * n));

  if (goalRed < startRed)
  {
    red = (byte)(startRed - redDiff);
  }
  else
  {
    red = (byte)(startRed + redDiff);
  }

  if (goalGreen < startGreen)
  {
    green = (byte)(startGreen - greenDiff);
  }
  else
  {
    green = (byte)(startGreen + greenDiff);
  }

  if (goalBlue < startBlue)
  {
    blue = (byte)(startBlue - blueDiff);
  }
  else
  {
    blue = (byte)(startBlue + blueDiff);
  }

  return strip.Color(red, green, blue);
}
