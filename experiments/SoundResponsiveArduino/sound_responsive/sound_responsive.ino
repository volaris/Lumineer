/* Chibi for Arduino, Example 4 
This example shows how to use the command line parser integrated into chibi. It's 
very useful for interactive control of a node, especially for testing and experimentation.
You can define your own commands to transmit, read registers, toggle I/O pins, etc. 

There's a function at the bottom called strCat(..) that might look kind of complicated.
It just takes in an array of strings and concatenates (connects) them together. This is 
because the command line parser chops up anything typed into the command line into a string array
with the space as the delimiter. The strCat function just takes the string array and puts them all back 
together.
*/

#include <chibi.h>
#include <Adafruit_NeoPixel.h>

#define PIN 9
#define NUM_PIXELS 30

enum effects {
  None, // 0 don't use animation, the controller will explicitely set the strip should look like
  Blank, // 1 this is really just a special case of SolidColor (0, 0, 0)
  SolidColor, // 2
  SolidColorFlash, // 3
  RainbowGradient, // 4
  SolidColorRainbowFade, // 5
  SolidColorTheaterChase, // 6
  RainbowGradientTheaterChase, // 7
  RainbowTheaterChase, // 8
  TwoColorSolidFade, // 9
  TwoColorGradient, // A
  Cylon, // B
  Phaser, // C cool with small width or large width or full width
  VU, // D
  RandomTwinkle, // E
  PulseFill, // F
  RandomFadingPulse, // 10
  TwinkleFlow //11
};

uint8_t bitmap1[(NUM_PIXELS/8)+1];
uint8_t bitmap2[(NUM_PIXELS/8)+1];

typedef union //all of the state data necessary for the effects
{
  struct //effects based on displaying a single color to the whole strip
  {
    uint32_t color; //current color to display
    struct
    {
      uint16_t interval; //how long should the algorithm wait between changing display
      uint32_t lastUpdate; //the millis() timeof the last change
      
      struct 
      {
        //TheaterChaseState - turn sequences of LEDs on and off
        uint8_t firstLEDState;
        uint8_t firstTransitionIndex;
        uint8_t numOn; // also used for left channel in VU
        uint8_t numOff; // also used for right channel in VU
        uint8_t chaseDirection;  // 0 : away from the controler, 1: towards the controler
        //RainbowFadeState - fade through a rainbow with a certain number of goal colors
        uint8_t numStops;
        uint8_t currentGoal; // this is also used for the flash effect - On: [color = color, currentGoal = blank] Off: [color = blank, currentGoal = color]
        // TwoColorSolidFadeState - fade between two colors
        uint32_t colors[2];
        uint8_t numSteps; // how many steps to take between colors when fading
        uint8_t currentStep;
      } AnimationSpecificState;
    } AnimationState;
  } SolidColorState;
  struct  //effects based on displaying a gradient of colors along the strip
  {
    uint16_t width;
    uint32_t startColor;
    
    struct 
    {
      uint16_t interval; //how long should the algorithm wait between changing display
      uint32_t lastUpdate; //the millis() timeof the last change
      uint8_t flowDirection;
      
      struct
      {
        uint8_t currentGoal; // what stop are we going towards
        // RainbowGradientState
        uint8_t numStops; // what size rainbow to use
        uint8_t numSteps; // how many steps between stops
        uint8_t startStep; // where we are in the sequence;
        //TwoColorGradientState
        uint32_t colors[2];
        uint8_t firstLEDState;
        uint8_t firstTransitionIndex;
        uint8_t numOn;
        uint8_t numOff;
        uint8_t chaseDirection;  // 0 : away from the controler, 1: towards the controler
      } AnimationSpecificState;
    } AnimationState;
  } GradientState;
} EffectState;

// Parameter 1 = number of pixels in strip
// Parameter 2 = Arduino pin number (most are valid)
// Parameter 3 = pixel type flags, add together as needed:
//   NEO_KHZ800  800 KHz bitstream (most NeoPixel products w/WS2812 LEDs)
//   NEO_KHZ400  400 KHz (classic 'v1' (not v2) FLORA pixels, WS2811 drivers)
//   NEO_GRB     Pixels are wired for GRB bitstream (most NeoPixel products)
//   NEO_RGB     Pixels are wired for RGB bitstream (v1 FLORA pixels, not v2)
Adafruit_NeoPixel strip = Adafruit_NeoPixel(NUM_PIXELS, PIN, NEO_GRB + NEO_KHZ800);
uint8_t color = 0;

uint32_t rainbow10[10];
uint32_t rainbow7[7];

typedef struct
{
  effects Effect;
  void (*init_function)(void); // this should initialize generated state from set state
  void (*animate_function)(void); // this should update the LEDs
} Effect;

Effect Effects[18] = {
  {None, NoneInit, NoneAnimate },
  {Blank, BlankInit, BlankAnimate },
  {SolidColor, SolidColorInit, SolidColorAnimate },
  {SolidColorFlash, SolidColorFlashInit, SolidColorFlashAnimate},
  {RainbowGradient, RainbowGradientInit, RainbowGradientAnimate},
  {SolidColorRainbowFade, SolidColorRainbowFadeInit, SolidColorRainbowFadeAnimate},
  {SolidColorTheaterChase, SolidColorTheaterChaseInit, SolidColorTheaterChaseAnimate},
  {RainbowGradientTheaterChase, RainbowGradientTheaterChaseInit, RainbowGradientTheaterChaseAnimate},
  {RainbowTheaterChase, RainbowTheaterChaseInit, RainbowTheaterChaseAnimate},
  {TwoColorSolidFade, TwoColorSolidFadeInit, TwoColorSolidFadeAnimate},
  {TwoColorGradient, TwoColorGradientInit, TwoColorGradientAnimate},
  {Cylon, CylonInit, CylonAnimate},
  {Phaser, PhaserInit, PhaserAnimate},
  {VU, VUInit, VUnimate},
  {RandomTwinkle, RandomTwinkleInit, RandomTwinkleAnimate},
  {PulseFill, PulseFillInit, PulseFillAnimate},
  {RandomFadingPulse, RandomFadingPulseInit, RandomFadingPulseAnimate},
  {TwinkleFlow, TwinkleFlowInit, TwinkleFlowAnimate}
};

EffectState currentState;
effects currentEffect = None;

/**************************************************************************/
// Initialize
/**************************************************************************/
void setup()
{  
  // Initialize the chibi command line and set the speed to 57600 bps
  chibiCmdInit(250000); //111111);//38400); //57600);
  
  // Initialize the chibi wireless stack
  chibiInit();
  
  rainbow10[0] = rainbow7[0] = strip.Color(0xFF, 0, 0);
  rainbow10[1] = rainbow7[1] = strip.Color(0xFF, 0xA5, 0);
  rainbow10[2] = rainbow7[2] = strip.Color(0xFF, 0xFF, 0);
  rainbow10[3] = rainbow7[3] = strip.Color(0, 0x80, 0);
  rainbow10[4] =               strip.Color(0, 0xFF, 0);
  rainbow10[5] =               strip.Color(0, 0xA5, 0x80);
  rainbow10[6] = rainbow7[4] = strip.Color(0, 0, 0xFF);
  rainbow10[7] = rainbow7[5] = strip.Color(0x4B, 0, 0x82);
  rainbow10[8] = rainbow7[6] = strip.Color(0xFF, 0, 0xFF);
  rainbow10[9] =  strip.Color(0xEE, 0x82, 0xEE);

  // This is where you declare the commands for the command line.
  // The first argument is the alias you type in the command line. The second
  // argument is the name of the function that the command will jump to.
  
  chibiCmdAdd("getsaddr", cmdGetShortAddr);  // set the short address of the node
  chibiCmdAdd("setsaddr", cmdSetShortAddr);  // get the short address of the node
  chibiCmdAdd("send", cmdSend);   // send the string typed into the command line
  chibiCmdAdd("nextColor", cmdNextColor); // move through a rainbow of colors
  chibiCmdAdd("setColor", cmdSetColor); // set specific color in a rainbow of colors
  chibiCmdAdd("setEffect", cmdSetEffect); // set a specific effect
  chibiCmdAdd("trigger", cmdTrigger); // trigger events in triggerable effects
  chibiCmdAdd("setPixel", cmdPixel);
  
  Serial.println("Started");
  
  strip.begin();
  strip.show(); // Initialize all pixels to 'off'

  currentEffect = RainbowGradient;//TwinkleFlow;//SolidColor;//SolidColorTheaterChase;//RainbowTheaterChase;//RandomFadingPulse;//
  
  currentState.GradientState.AnimationState.AnimationSpecificState.numStops = 7;
  currentState.GradientState.AnimationState.AnimationSpecificState.numSteps = 0xF;
  currentState.GradientState.AnimationState.interval = 0xA0;
  
  /*currentState.SolidColorState.color = strip.Color(0x0, 0x0, 0x3F);
  currentState.SolidColorState.AnimationState.interval = 45;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = 3;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = 4;
  //currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops = 7;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps = 0x1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection = 0;
  */
  Effects[currentEffect].init_function();
}

// Fill the dots one after the other with a color
void colorWipe(uint32_t c, uint8_t wait) {
  for(uint16_t i=0; i<strip.numPixels(); i++) {
      strip.setPixelColor(i, c);
      if(wait > 0)
      {
        strip.show();
      }
      delay(wait);
  }
  if(wait == 0)
  {
    strip.show();
  }
}

/**************************************************************************/
// Loop
/**************************************************************************/
void loop()
{
  // This function checks the command line to see if anything new was typed.
  // This needs to be called before animate() as it is where the init funciton is called.
  chibiCmdPoll();
  
  Effects[currentEffect].animate_function();
    
  // Check if any data was received from the radio. If so, then handle it.
  if (chibiDataRcvd() == true)
  { 
    int len, rssi, src_addr;
    byte buf[100];  // this is where we store the received data
    
    // retrieve the data and the signal strength
    len = chibiGetData(buf);

    // discard the data if the length is 0. that means its a duplicate packet
    if (len == 0) return;    

    rssi = chibiGetRSSI();
    src_addr = chibiGetSrcAddr();
    
    // Print out the message and the signal strength
    Serial.print("Message received from node 0x");
    Serial.print(src_addr, HEX);
    Serial.print(": "); 
    Serial.print((char *)buf); 
    Serial.print(", RSSI = 0x"); Serial.println(rssi, HEX);
  }
}

/**************************************************************************/
// USER FUNCTIONS
/**************************************************************************/

/**************************************************************************/
/*!
    Get short address of device from EEPROM
    Usage: getsaddr
*/
/**************************************************************************/
void cmdGetShortAddr(int arg_cnt, char **args)
{
  int val;
  
  val = chibiGetShortAddr();
  Serial.print("Short Address: "); Serial.println(val, HEX);
}

/**************************************************************************/
/*!
    Write short address of device to EEPROM
    Usage: setsaddr <addr>
*/
/**************************************************************************/
void cmdSetShortAddr(int arg_cnt, char **args)
{
  int val;
  
  val = chibiCmdStr2Num(args[1], 16);
  chibiSetShortAddr(val);
}

/**************************************************************************/
/*!
    Transmit data to another node wirelessly using Chibi stack. Currently
    only handles ASCII string payload
    Usage: send <addr> <string...>
*/
/**************************************************************************/
void cmdSend(int arg_cnt, char **args)
{
    byte data[100];
    int addr, len;

    // convert cmd line string to integer with specified base
    addr = chibiCmdStr2Num(args[1], 16);
    
    // concatenate strings typed into the command line and send it to
    // the specified address
    len = strCat((char *)data, 2, arg_cnt, args);    
    chibiTx(addr, data,len);
}

/**************************************************************************/
/*!
    Move one forward in R->G->B->R
    Usage: nextColor
*/
/**************************************************************************/
void cmdNextColor(int arg_cnt, char **args)
{
    Serial.println("Updating Color");
    color = (color + 1) % 3;
    if(color == 0)
    {
      currentState.SolidColorState.color = strip.Color(255, 0, 0); // Red
    }
    else if(color == 1)
    {
      currentState.SolidColorState.color = strip.Color(0, 255, 0); // Green
    }
    else if(color == 2)
    {
      currentState.SolidColorState.color = strip.Color(0, 0, 255); // Blue
    }
    
    currentEffect = SolidColor;
    currentState.SolidColorState.AnimationState.interval = 100;
    currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = 1;
    currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = 2;
    Effects[currentEffect].init_function();
}

/**************************************************************************/
/*!
    set an individual pixel color
    Usage: nextColor <pixelNum> <color>
*/
/**************************************************************************/
void cmdPixel(int arg_cnt, char **args)
{
  uint16_t pixel;
  uint32_t color;
  
  if(arg_cnt < 3)
  {
    Serial.println("Incorrect number of arguments");
    return;
  }
  
  pixel = chibiCmdStr2Num(args[1], 16);
  color = chibiCmdStr2Num(args[2], 16);
  
  strip.setPixelColor(pixel, color);
  strip.show();
}

/**************************************************************************/
/*!
    Set which color on the color wheel
    Usage: setColor <colorIndex>
*/
/**************************************************************************/
void cmdSetColor(int arg_cnt, char **args)
{
    int i;
    uint32_t colors[10];
    
    if(arg_cnt < 2)
    {
      Serial.println("Incorrect number of arguments");
      return;
    }
    
    colors[0] = strip.Color(0xFF, 0, 0);
    colors[1] = strip.Color(0xFF, 0xA5, 0);
    colors[2] = strip.Color(0xFF, 0xFF, 0);
    colors[3] = strip.Color(0, 0x80, 0);
    colors[4] = strip.Color(0, 0xFF, 0);
    colors[5] = strip.Color(0, 0xA5, 0x80);
    colors[6] = strip.Color(0, 0, 0xFF);
    colors[7] = strip.Color(0x4B, 0, 0x82);
    colors[8] = strip.Color(0xFF, 0, 0xFF);
    colors[9] = strip.Color(0xEE, 0x82, 0xEE);
  
    // convert cmd line string to integer with specified base
    i = chibiCmdStr2Num(args[1], 16);
    
    currentState.SolidColorState.color = colors[i];
    currentEffect = SolidColorTheaterChase;
    currentState.SolidColorState.AnimationState.interval = 100;
    currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = 1;
    currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = 2;
    Effects[currentEffect].init_function();
}

/**************************************************************************/
/*!
    Set which effect to run and the parameters for it
    Usage: setEffect <EffectIndex>
*/
/**************************************************************************/
void cmdSetEffect(int arg_cnt, char **args)
{
    uint32_t effect, color, alternateColor, interval;
    if(arg_cnt < 2)
    {
      Serial.println("Incorrect number of arguments");
      return;
    }
    
    effect = chibiCmdStr2Num(args[1], 16);
    
    switch(effect)
    {
      case None:
      case Blank: //nothing needs to be done for either
        break;
      case SolidColor: 
        if(arg_cnt < 3)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.color = chibiCmdStr2Num(args[2], 16);
        break;
      case SolidColorFlash:
        if(arg_cnt < 4)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.color = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[3], 16);
        break;
      case PulseFill:
      case RainbowGradient:
        if(arg_cnt < 5)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.GradientState.AnimationState.AnimationSpecificState.numStops = chibiCmdStr2Num(args[2], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.numSteps = chibiCmdStr2Num(args[3], 16);
        currentState.GradientState.AnimationState.interval = chibiCmdStr2Num(args[4], 16);
        break;
      case SolidColorRainbowFade:
        if(arg_cnt < 5)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps = chibiCmdStr2Num(args[3], 16);
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[4], 16);
        break;
      case SolidColorTheaterChase:
        if(arg_cnt < 6)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.color = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[3], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = chibiCmdStr2Num(args[4], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = chibiCmdStr2Num(args[5], 16);
        break;
      case RainbowGradientTheaterChase:
        if(arg_cnt < 7)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.GradientState.AnimationState.interval = chibiCmdStr2Num(args[2], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.numOn = chibiCmdStr2Num(args[3], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.numOff = chibiCmdStr2Num(args[4], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.numStops = chibiCmdStr2Num(args[5], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.numSteps = chibiCmdStr2Num(args[6], 16);
        break;
      case RainbowTheaterChase:
        if(arg_cnt < 6)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = chibiCmdStr2Num(args[3], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = chibiCmdStr2Num(args[4], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops = chibiCmdStr2Num(args[5], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps = chibiCmdStr2Num(args[6], 16);
        break;
      case TwoColorSolidFade:
        if(arg_cnt < 6)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.AnimationState.AnimationSpecificState.colors[0] = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.colors[1] = chibiCmdStr2Num(args[3], 16);
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[4], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps = chibiCmdStr2Num(args[5], 16);
        break;
      case TwoColorGradient:
        break;
      case Cylon:
        if(arg_cnt < 6)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = chibiCmdStr2Num(args[2], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.colors[1] = chibiCmdStr2Num(args[3], 16);
        currentState.GradientState.AnimationState.interval = chibiCmdStr2Num(args[4], 16);
        currentState.GradientState.width = chibiCmdStr2Num(args[5], 16);
        break;
      case Phaser:
        if(arg_cnt < 7)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = chibiCmdStr2Num(args[2], 16);
        currentState.GradientState.AnimationState.AnimationSpecificState.colors[1] = chibiCmdStr2Num(args[3], 16);
        currentState.GradientState.AnimationState.interval = chibiCmdStr2Num(args[4], 16);
        currentState.GradientState.width = chibiCmdStr2Num(args[5], 16);
        currentState.GradientState.AnimationState.flowDirection = chibiCmdStr2Num(args[6], 16);
        if(currentState.GradientState.AnimationState.flowDirection > 1 && (strip.numPixels() / (currentState.GradientState.AnimationState.flowDirection)) < 20)
        {
          Serial.println("not enough pixels for given flow, reducing");
          currentState.GradientState.AnimationState.flowDirection = strip.numPixels() / 20;
        }
        currentState.GradientState.AnimationState.AnimationSpecificState.numStops = chibiCmdStr2Num(args[7], 16);
        break;
      case VU:
        if(arg_cnt < 3)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.color = chibiCmdStr2Num(args[2], 16);
        break;
      case RandomTwinkle:
        if(arg_cnt < 6)
        {
          Serial.println("Incorrect number of arguments");
          return;
        }
        currentState.SolidColorState.color = chibiCmdStr2Num(args[2], 16);
        currentState.SolidColorState.AnimationState.interval = chibiCmdStr2Num(args[3], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = chibiCmdStr2Num(args[4], 16);
        currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = chibiCmdStr2Num(args[5], 16);
        break;
      case RandomFadingPulse:
        break;
      default:
        Serial.println("Invalid effect ID");
        return;
    }
    
    currentEffect = (effects)effect;
    Effects[currentEffect].init_function();
}

/**************************************************************************/
/*!
    Trigger events in triggerable effects
    Usage: trigger
*/
/**************************************************************************/
void cmdTrigger(int arg_cnt, char **args)
{   
  switch(currentEffect)
  {
    case None:
    case Blank:
    case SolidColor:
    case SolidColorFlash:
    case RainbowGradient:
    case SolidColorRainbowFade:
    case SolidColorTheaterChase:
    case RainbowGradientTheaterChase:
    case RainbowTheaterChase:
    case TwoColorSolidFade:
    case TwoColorGradient:
    case Cylon:
    case RandomFadingPulse:
      break;
    case Phaser:
      currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = (currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops;
      if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops > 0)
      {
        if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 10)
        {
          currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = rainbow10[currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal];
        }
        else
        {
          currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = rainbow7[currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal];
        }
      }
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep = currentState.GradientState.width;
      break;
    case VU:
      if(arg_cnt < 3)
      {
        break;
      }
      currentState.SolidColorState.AnimationState.lastUpdate = 0;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = chibiCmdStr2Num(args[1], 16);
      currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = chibiCmdStr2Num(args[2], 16);
      break;
  }
}

/**************************************************************************/
/*!
    Concatenate multiple strings from the command line starting from the
    given index into one long string separated by spaces.
*/
/**************************************************************************/
int strCat(char *buf, unsigned char index, char arg_cnt, char **args)
{
    uint8_t i, len;
    char *data_ptr;

    data_ptr = buf;
    for (i=0; i<arg_cnt - index; i++)
    {
        len = strlen(args[i+index]);
        strcpy((char *)data_ptr, (char *)args[i+index]);
        data_ptr += len;
        *data_ptr++ = ' ';
    }
    *data_ptr++ = '\0';

    return data_ptr - buf;
}

void NoneInit(void)
{
}

void NoneAnimate (void)
{
}

void BlankInit(void)
{
  currentState.SolidColorState.color = strip.Color(0, 0, 0);
}

void BlankAnimate (void)
{
  colorWipe(currentState.SolidColorState.color, 0);
}

void SolidColorInit(void)
{
  return;
}

void SolidColorAnimate(void)
{
  colorWipe(currentState.SolidColorState.color, 0);
}

void SolidColorFlashInit(void)
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 1;
  return;
}

void SolidColorFlashAnimate(void)
{
  uint32_t time = millis();
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal == 0)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 1;
      colorWipe(0, 0);
    }
    else
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 0;
      colorWipe(currentState.SolidColorState.color, 0);
    }
  }
}

void RainbowGradientInit(void)
{
  currentState.GradientState.AnimationState.lastUpdate = millis() - currentState.GradientState.AnimationState.interval;
  currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = 1; //starting at red going to orange
  currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
}

void RainbowGradientAnimate(void)
{
  uint32_t time = millis();
  if(time - currentState.GradientState.AnimationState.lastUpdate >= currentState.GradientState.AnimationState.interval)
  {
    currentState.GradientState.AnimationState.lastUpdate = time;
    uint32_t* rainbow;
    uint8_t localGoal;
    uint16_t localStep;
    uint8_t i;
    uint32_t goal;
    uint32_t start;
    uint32_t current;
    localGoal = currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal;
    if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 7)
    {
      rainbow = rainbow7;
    }
    else if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      rainbow = rainbow10;
    }
    else
    {
      Serial.println("invalid number of stops");
    }
    
    localStep = currentState.GradientState.AnimationState.AnimationSpecificState.startStep;
    
    for(i = 0; i < strip.numPixels(); i++)
    {
      goal = rainbow[localGoal];
      if(localGoal > 0)
      {
        start = rainbow[localGoal - 1];
      }
      else
      {
        start = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.numStops - 1];
      }
      current = GetGradientColor(start, goal, currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, localStep);
      
      //Serial.println(current, HEX);
      strip.setPixelColor(i, current);
      if(localStep == currentState.GradientState.AnimationState.AnimationSpecificState.numSteps)
      {
        localStep = 0;
        localGoal = (localGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops; 
        //set next goal
      }
      else
      {
        localStep++;
      }
    }
    
    if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep == currentState.GradientState.AnimationState.AnimationSpecificState.numSteps)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
      currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = (currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops; 
      //set next goal
    }
    else
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep++;
    }    
    strip.show();
  }
}

void PulseFillInit(void)
{
  currentState.GradientState.AnimationState.lastUpdate = millis() - currentState.GradientState.AnimationState.interval;
  currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = 1; //starting at red going to orange
  currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
}

void PulseFillAnimate(void)
{
  uint32_t time = millis();
  if(time - currentState.GradientState.AnimationState.lastUpdate >= currentState.GradientState.AnimationState.interval)
  {
    currentState.GradientState.AnimationState.lastUpdate = time;
    uint32_t* rainbow;
    uint8_t localGoal;
    uint16_t localStep;
    uint8_t i;
    uint32_t goal;
    uint32_t start;
    uint32_t current;
    localGoal = currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal;
    if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 7)
    {
      rainbow = rainbow7;
    }
    else if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      rainbow = rainbow10;
    }
    else
    {
      Serial.println("invalid number of stops");
    }
    if(currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal > 0)
    {
      start = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal - 1];
    }
    else
    {
      start = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.numStops - 1];
    }
    localStep = currentState.GradientState.AnimationState.AnimationSpecificState.startStep;
    
    for(i = 0; i < strip.numPixels(); i++)
    {
      goal = rainbow[localGoal];
      current = GetGradientColor(start, goal, localStep, currentState.GradientState.AnimationState.AnimationSpecificState.numSteps);
      strip.setPixelColor(i, current);
      if(localStep == currentState.GradientState.AnimationState.AnimationSpecificState.numSteps)
      {
        localStep = 0;
        localGoal = (currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops; 
        //set next goal
      }
      else
      {
        localStep++;
      }
    }
    
    if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep == currentState.GradientState.AnimationState.AnimationSpecificState.numSteps)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
      currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = (currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops; 
      //set next goal
    }
    else
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep++;
    }
    strip.show();
  }

}

void SolidColorRainbowFadeInit(void)
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
  {
    currentState.SolidColorState.color = rainbow7[0];
  }
  else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
  {
    currentState.SolidColorState.color = rainbow10[0];
  }
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 1;
}

void SolidColorRainbowFadeAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    uint32_t start, goal, current;
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    start = currentState.SolidColorState.color;
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
    {
      goal = rainbow7[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
    }
    else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      goal = rainbow10[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
    }
    
    current = GetGradientColor(start, goal, currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps, currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep);
    
    colorWipe(current, 0);
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep == currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps)
    {
      if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
      {
        currentState.SolidColorState.color = rainbow7[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
      }
      else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
      {
        currentState.SolidColorState.color = rainbow10[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
      }
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = (currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
    }
    else
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep++;
    }
  }
}

void SolidColorTheaterChaseInit(void)
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  
  currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection = 1;
  return;
}

void SolidColorTheaterChaseAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    uint8_t on = currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState;
    uint8_t numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex;
    
    if(numLeft == 0)
    {
      if(on)
      {
         numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn;
      }
      else 
      {
         numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff;
      }
    }
    currentState.SolidColorState.AnimationState.lastUpdate = millis();
    
    for(uint8_t i = 0; i < strip.numPixels(); i++)
    {
      if(on)
      {
        numLeft--;
        if(currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection == 0)
        {
          strip.setPixelColor(i, currentState.SolidColorState.color);
        }
        else
        {
          strip.setPixelColor(strip.numPixels() - i - 1, currentState.SolidColorState.color);
        }
        if(numLeft == 0)
        {
          on = 0;
          numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff;
        }
      }
      else
      {
        numLeft--;
        if(currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection == 0)
        {
          strip.setPixelColor(i, 0);
        }
        else
        {
          strip.setPixelColor(strip.numPixels() - i - 1, 0);
        }
        if(numLeft == 0)
        {
          on = 1;
          numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn;
        }
      }
    }
    currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex++;
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState && 
       currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 0;
    }
    else if(!currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState && 
             currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 1;
    }
    strip.show();
  }
}

void RainbowGradientTheaterChaseInit(void)
{
  currentState.GradientState.AnimationState.lastUpdate = millis() - currentState.GradientState.AnimationState.interval;
  currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = 1; //starting at red going to orange
  currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
  currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState = 1;
  currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
  currentState.GradientState.AnimationState.AnimationSpecificState.chaseDirection = 1;
}

void RainbowGradientTheaterChaseAnimate(void)
{
  uint32_t time = millis();
  if(time - currentState.GradientState.AnimationState.lastUpdate >= currentState.GradientState.AnimationState.interval)
  {
    currentState.GradientState.AnimationState.lastUpdate = time;
    uint8_t red, green, blue, goalRed, goalGreen, goalBlue, startRed, startGreen, startBlue, totalRedDiff, totalGreenDiff, totalBlueDiff, i, localGoal;
    uint16_t redDelta, greenDelta, blueDelta;
    uint32_t* rainbow;
    uint32_t goal;
    localGoal = currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal;
    uint32_t start;
    if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 7)
    {
      rainbow = rainbow7;
    }
    else if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      rainbow = rainbow10;
    }
    else
    {
      Serial.println("invalid number of stops");
    }
    if(currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal > 0)
    {
      start = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal - 1];
    }
    else
    {
      start = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.numStops - 1];
    }
    goal = rainbow[currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal];
      
    goalRed = (0x00FF0000 & goal) >> 16;
    goalGreen = (0x0000FF00 & goal) >> 8;
    goalBlue = (0x000000FF & goal);
      
    startRed = (0x00FF0000 & start) >> 16;
    startGreen = (0x0000FF00 & start) >> 8;
    startBlue = (0x000000FF & start);
    
    totalRedDiff = abs(goalRed - startRed);
    totalGreenDiff = abs(goalGreen - startGreen);
    totalBlueDiff = abs(goalBlue - startBlue);
    
    red = startRed;
    green = startGreen;
    blue = startBlue;
    
    redDelta = (((uint16_t)currentState.GradientState.AnimationState.AnimationSpecificState.startStep * totalRedDiff) / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps);
    greenDelta = (((uint16_t)currentState.GradientState.AnimationState.AnimationSpecificState.startStep * totalGreenDiff) / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps);
    blueDelta = (((uint16_t)currentState.GradientState.AnimationState.AnimationSpecificState.startStep * totalBlueDiff) / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps);
    
    if(redDelta >= totalRedDiff)
    {
      red = goalRed;
    }
    else
    {
      if(goalRed > startRed)
      {
        red += redDelta;
      }
      else
      {
        red -= redDelta;
      }
    }
    
    if(greenDelta >= totalGreenDiff)
    {
      green = goalGreen;
    }
    else
    {
      if(goalGreen > startGreen)
      {
        green += greenDelta;
      }
      else
      {
        green -= greenDelta;
      }
    }
    
    if(blueDelta >= totalBlueDiff)
    {
      blue = goalBlue;
    }
    else
    {
      if(goalBlue > startBlue)
      {
        blue += blueDelta;
      }
      else
      {
        blue -= blueDelta;
      }
    }
    
    strip.setPixelColor(0, red, green, blue);
     
    //start conditions set
    //generate the rest of the gradient
    redDelta = max(totalRedDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
    greenDelta = max(totalGreenDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
    blueDelta = max(totalBlueDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
    
    for(i = 1; i < strip.numPixels(); i++)
    {
      if(goalRed == red && goalGreen == green && goalBlue == blue)
      {
        //set next local goal
        localGoal = (localGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops;
        
        goal = rainbow[localGoal];
      
        goalRed = (0x00FF0000 & goal) >> 16;
        goalGreen = (0x0000FF00 & goal) >> 8;
        goalBlue = (0x000000FF & goal);
    
        totalRedDiff = abs(goalRed - red);
        totalGreenDiff = abs(goalGreen - green);
        totalBlueDiff = abs(goalBlue - blue);
        
        redDelta = max(totalRedDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
        greenDelta = max(totalGreenDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
        blueDelta = max(totalBlueDiff / currentState.GradientState.AnimationState.AnimationSpecificState.numSteps, 1);
      }
      
      if(abs(goalRed - red) < redDelta)
      {
        red = goalRed;
      }
      else if(goalRed > red)
      {
        red += redDelta;
      }
      else if(goalRed < red)
      {
        red -= redDelta;
      }
      
      if(abs(goalGreen - green) < greenDelta)
      {
        green = goalGreen;
      }
      else if(goalGreen > green)
      {
        green += greenDelta;
      }
      else if(goalGreen < green)
      {
        green -= greenDelta;
      }
      
      if(abs(goalBlue - blue) < blueDelta)
      {
        blue = goalBlue;
      }
      else if(goalBlue > blue)
      {
        blue += blueDelta;
      }
      else if(goalBlue < blue)
      {
        blue -= blueDelta;
      }

      strip.setPixelColor(i, red, green, blue);
    }
    
    if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep == currentState.GradientState.AnimationState.AnimationSpecificState.numSteps)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
      currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = (currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.GradientState.AnimationState.AnimationSpecificState.numStops; 
      //set next goal
    }
    else
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.startStep++;
    }
    
    uint8_t on = currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState;
    uint8_t numLeft = currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex;
    
    if(numLeft == 0)
    {
      if(on)
      {
         numLeft = currentState.GradientState.AnimationState.AnimationSpecificState.numOn;
      }
      else 
      {
         numLeft = currentState.GradientState.AnimationState.AnimationSpecificState.numOff;
      }
    }
    
    for(uint8_t i = 0; i < strip.numPixels(); i++)
    {
      if(on)
      {
        numLeft--;
        if(numLeft == 0)
        {
          on = 0;
          numLeft = currentState.GradientState.AnimationState.AnimationSpecificState.numOff;
        }
      }
      else
      {
        numLeft--;
        if(currentState.GradientState.AnimationState.AnimationSpecificState.chaseDirection == 0)
        {
          strip.setPixelColor(i, 0);
        }
        else
        {
          strip.setPixelColor(strip.numPixels() - i - 1, 0);
        }
        if(numLeft == 0)
        {
          on = 1;
          numLeft = currentState.GradientState.AnimationState.AnimationSpecificState.numOn;
        }
      }
    }
    currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex++;
    if(currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState && 
       currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState = 0;
    }
    else if(!currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState && 
             currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.GradientState.AnimationState.AnimationSpecificState.firstLEDState = 1;
    }
    
    strip.show();
  }
}

void RainbowTheaterChaseInit(void)
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
  {
    currentState.SolidColorState.color = rainbow7[0];
  }
  else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
  {
    currentState.SolidColorState.color = rainbow10[0];
  }
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection = 1;
}

void RainbowTheaterChaseAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    uint32_t start, goal, current;
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    start = currentState.SolidColorState.color;
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
    {
      goal = rainbow7[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
    }
    else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      goal = rainbow10[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
    }
    
    current = GetGradientColor(start, goal, currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps, currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep);
    
    uint8_t on = currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState;
    uint8_t numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex;
    
    if(numLeft == 0)
    {
      if(on)
      {
         numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn;
      }
      else 
      {
         numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff;
      }
    }
    currentState.SolidColorState.AnimationState.lastUpdate = millis();
    
    for(uint8_t i = 0; i < strip.numPixels(); i++)
    {
      if(on)
      {
        numLeft--;
        if(currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection == 0)
        {
          strip.setPixelColor(i, current);
        }
        else
        {
          strip.setPixelColor(strip.numPixels() - i - 1, current);
        }
        if(numLeft == 0)
        {
          on = 0;
          numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff;
        }
      }
      else
      {
        numLeft--;
        if(currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection == 0)
        {
          strip.setPixelColor(i, 0);
        }
        else
        {
          strip.setPixelColor(strip.numPixels() - i - 1, 0);
        }
        if(numLeft == 0)
        {
          on = 1;
          numLeft = currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn;
        }
      }
    }
    currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex++;
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState && 
       currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 0;
    }
    else if(!currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState && 
             currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex > currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstTransitionIndex = 1;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.firstLEDState = 1;
    }
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep == currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps)
    {
      if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 7)
      {
        currentState.SolidColorState.color = rainbow7[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
      }
      else if(currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops == 10)
      {
        currentState.SolidColorState.color = rainbow10[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
      }
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = (currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal + 1) % currentState.SolidColorState.AnimationState.AnimationSpecificState.numStops;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
    }
    else
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep++;
    }
    
    strip.show();
  }
}

void TwoColorSolidFadeInit(void)
{  
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = 1;
  currentState.SolidColorState.color = currentState.SolidColorState.AnimationState.AnimationSpecificState.colors[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
}

void TwoColorSolidFadeAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    uint32_t goal = currentState.SolidColorState.AnimationState.AnimationSpecificState.colors[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal];
    uint32_t start = currentState.SolidColorState.AnimationState.AnimationSpecificState.colors[currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal ? 0 : 1];
    
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    currentState.SolidColorState.color = GetGradientColor(start, goal, currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps, currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep);//strip.Color(red, green, blue);
    
    colorWipe(currentState.SolidColorState.color, 0);
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep == currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps)
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal = currentState.SolidColorState.AnimationState.AnimationSpecificState.currentGoal ? 0 : 1;
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
    }
    else
    {
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep++;
    }
  }
}

void TwoColorGradientInit(void)
{
}

void TwoColorGradientAnimate(void)
{
}

void CylonInit(void)
{  
  uint8_t i = 0;
  currentState.GradientState.AnimationState.lastUpdate = millis() - currentState.GradientState.AnimationState.interval;
          
  for(i = 0; i < currentState.GradientState.width; i++)
  {
    strip.setPixelColor(i, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
  }    
  for(; i < strip.numPixels(); i++)
  {
    strip.setPixelColor(i, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
  }
  currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
  strip.show();
}

void CylonAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.GradientState.AnimationState.lastUpdate >= currentState.GradientState.AnimationState.interval)
  {
    currentState.GradientState.AnimationState.lastUpdate = time;
    if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep < (strip.numPixels() - currentState.GradientState.width))
    {
      uint8_t i = 0;
      uint8_t* pixels = strip.getPixels();        
      for(i = strip.numPixels() - 1; i > 0 ; i--)
      {
        strip.setPixelColor(i, strip.getPixelColor(i-1));
      }
      strip.setPixelColor(0, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
    }
    else
    {
      uint8_t i = 0;
      uint8_t* pixels = strip.getPixels();        
      for(i = 0; i < strip.numPixels() - 1; i++)
      {
        strip.setPixelColor(i, strip.getPixelColor(i+1));
      }
      strip.setPixelColor(strip.numPixels() - 1, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
    }
    currentState.GradientState.AnimationState.AnimationSpecificState.startStep = (currentState.GradientState.AnimationState.AnimationSpecificState.startStep + 1) % ((2 * strip.numPixels()) - (2 * currentState.GradientState.width));
    strip.show();
  }
}

void PhaserInit(void)
{  
  uint8_t i = 0;
  currentState.GradientState.AnimationState.lastUpdate = millis() - currentState.GradientState.AnimationState.interval;
  
  for(i = 0; i < strip.numPixels(); i++)
  {
    strip.setPixelColor(i, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
  }
  currentState.GradientState.AnimationState.AnimationSpecificState.startStep = 0;
  currentState.GradientState.AnimationState.AnimationSpecificState.currentGoal = 0;
  if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops > 0)
  {
    if(currentState.GradientState.AnimationState.AnimationSpecificState.numStops == 10)
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = rainbow10[0];
    }
    else
    {
      currentState.GradientState.AnimationState.AnimationSpecificState.colors[0] = rainbow7[0];
    }
  }
  strip.show();
}

void PhaserAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.GradientState.AnimationState.lastUpdate >= currentState.GradientState.AnimationState.interval)
  {
    currentState.GradientState.AnimationState.lastUpdate = time;
    if(currentState.GradientState.AnimationState.flowDirection == 0)
    {
      uint8_t i = 0;    
      for(i = strip.numPixels() - 1; i > 0 ; i--)
      {
        strip.setPixelColor(i, strip.getPixelColor(i-1));
      }
      if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep > 0)
      {
        strip.setPixelColor(0, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
        currentState.GradientState.AnimationState.AnimationSpecificState.startStep--;
      }
      else
      {
        strip.setPixelColor(0, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
      }
    }
    else if(currentState.GradientState.AnimationState.flowDirection == 1)
    {
      uint8_t i = 0;       
      for(i = 0; i < strip.numPixels() - 1; i++)
      {
        strip.setPixelColor(i, strip.getPixelColor(i+1));
      }
      if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep > 0)
      {
        strip.setPixelColor(strip.numPixels() - 1, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
        currentState.GradientState.AnimationState.AnimationSpecificState.startStep--;
      }
      else
      {
        strip.setPixelColor(strip.numPixels() - 1, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
      }
    }
    else
    {
      uint8_t numSegments = currentState.GradientState.AnimationState.flowDirection;
      uint8_t dir = 1;
      uint16_t i, j;
      uint16_t pixelsPerSegment = strip.numPixels() / numSegments;
      
      for(i = 0; i < numSegments; i++)
      {
        for(j = 0; j < pixelsPerSegment - 1; j++)
        {
          if(dir == 0)
          {
            uint16_t pixelIndex = (i * pixelsPerSegment) + (pixelsPerSegment - j); 
            strip.setPixelColor(pixelIndex, strip.getPixelColor(pixelIndex-1));
          }
          else
          {
            uint16_t pixelIndex = (i * pixelsPerSegment) + (j); 
            strip.setPixelColor(pixelIndex, strip.getPixelColor(pixelIndex+1));
          }
        }
        
        if(dir == 0)
        {
          uint16_t pixelIndex = (i * pixelsPerSegment);// + (pixelsPerSegment - j); 
          if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep > 0)
          {
            strip.setPixelColor(pixelIndex, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
            strip.setPixelColor(pixelIndex+1, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
          }
          else
          {
            strip.setPixelColor(pixelIndex, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
            strip.setPixelColor(pixelIndex+1, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
          }
        }
        else
        {
          uint16_t pixelIndex = (i * pixelsPerSegment) + (j); 
          if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep > 0)
          {
            strip.setPixelColor(pixelIndex, currentState.GradientState.AnimationState.AnimationSpecificState.colors[0]);
          }
          else
          {
            strip.setPixelColor(pixelIndex, currentState.GradientState.AnimationState.AnimationSpecificState.colors[1]);
          }
        }
        
        if(currentState.GradientState.AnimationState.AnimationSpecificState.startStep > 0)
        {
          currentState.GradientState.AnimationState.AnimationSpecificState.startStep--;
        }
        dir = (dir + 1) % 2;
      }
    }
    strip.show();
  }
}

void VUInit()
{
  currentState.SolidColorState.AnimationState.lastUpdate = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff = 0;
}

void VUnimate()
{
  uint32_t time = millis();
  uint16_t i;
  if(currentState.SolidColorState.AnimationState.lastUpdate == 0)
  {
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    uint16_t left = ((strip.numPixels() / 2) * currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn) / 0xFF;
    uint16_t right = ((strip.numPixels() / 2) * currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff) / 0xFF;
    
    colorWipe(0, 0);
    
    for(i = 0; i < strip.numPixels(); i++)
    {
      if(i < strip.numPixels() / 2)
      {
        if(i >= ((strip.numPixels() / 2) - (right)))
        {
          strip.setPixelColor(i, currentState.SolidColorState.color);
        }
      }
      else
      {
        if(i < ((strip.numPixels()/2) + left))
        {
          strip.setPixelColor(i, currentState.SolidColorState.color);
        }
      }
    }
    strip.show();
  }
}

void RandomTwinkleInit()
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
}

void RandomTwinkleAnimate()
{
  uint32_t time = millis();
  uint16_t i;
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    colorWipe(0, 0);
    
    for(i = 0; i < strip.numPixels(); i++)
    {
      uint8_t rand = random(currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff + 1);
      
      if(rand <= currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn)
      {
        strip.setPixelColor(i, currentState.SolidColorState.color);
      }
    }
    strip.show();
  }
}

void RandomFadingPulseInit()
{
  currentState.SolidColorState.AnimationState.lastUpdate = 0;
  currentState.SolidColorState.color = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps = 10;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.chaseDirection = 0; 
  currentState.SolidColorState.AnimationState.interval = 100;
  
  colorWipe(0,0);
}

void RandomFadingPulseAnimate()
{
  uint32_t time = millis();
  uint8_t i = 0;
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    for(i = strip.numPixels() - 1; i > 0; i--)
    {
      strip.setPixelColor(i, strip.getPixelColor(i-1));
    }
    
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep == 0)
    {
      uint8_t rand = random(50);
      if(rand == 0)
      {
        currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps;
        currentState.SolidColorState.color = GetRandomVibrantColor();
        strip.setPixelColor(0, currentState.SolidColorState.color);
      }
      else
      {
        strip.setPixelColor(0, 0);
      }
    }
    else
    {
      uint16_t gradientStep = currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps - currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep;
      uint32_t color = GetGradientColor(currentState.SolidColorState.color, 0, currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps, gradientStep);
      strip.setPixelColor(0, color);
      currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep--;
    }
    strip.show();
  }
}

uint32_t GetRandomVibrantColor()
{
  uint8_t randSegment = random(6);
  uint8_t red, green, blue;
  uint8_t rand = random(256);
  switch(randSegment)
  {
    case 0:
      red = 0xFF;
      green = rand;
      break;
    case 1:
      green = 0xFF;
      red = rand;
      break;
    case 2:
      green = 0xFF;
      blue = rand;
      break;
    case 3:
      blue = 0xFF;
      green = rand;
      break;
    case 4:
      blue = 0xFF;
      red = rand;
      break;
    case 5:
      red = 0xFF;
      blue = rand;
      break;
  }
  return strip.Color(red, green, blue);
}

uint32_t GetGradientColor(uint32_t start, uint32_t goal, uint16_t numSteps, uint16_t currentStep)
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
  
  redDiff = (redDelta * currentStep) / numSteps;
  blueDiff = (blueDelta * currentStep) / numSteps;
  greenDiff = (greenDelta * currentStep) / numSteps;
  
  if(goalRed < startRed)
  {
    red = startRed - redDiff;
  }
  else
  {
    red = startRed + redDiff;
  }
  
  if(goalGreen < startGreen)
  {
    green = startGreen - greenDiff;
  }
  else
  {
    green = startGreen + greenDiff;
  }
  
  if(goalBlue < startBlue)
  {
    blue = startBlue - blueDiff;
  }
  else
  {
    blue = startBlue + blueDiff;
  }
  
  return strip.Color(red, green, blue);
}


void TwinkleFlowInit(void)
{
  currentState.SolidColorState.AnimationState.lastUpdate = millis() - currentState.SolidColorState.AnimationState.interval;
  currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = 0;
}

void TwinkleFlowAnimate(void)
{
  uint32_t time = millis();
  
  if(time - currentState.SolidColorState.AnimationState.lastUpdate >= currentState.SolidColorState.AnimationState.interval)
  {
    currentState.SolidColorState.AnimationState.lastUpdate = time;
    
    Serial.println("Animate");
    
    // display current state
    for(uint8_t i = 0; i < strip.numPixels(); i++)
    {
      uint8_t byte_num = i / 8;
      uint8_t bit_num = i % 8;
      
      if(((bitmap2[byte_num] & (1 << bit_num)) == 0) && ((bitmap1[byte_num] & (1 << bit_num)) != 0))
      {
        strip.setPixelColor(i, currentState.SolidColorState.color);
      }
      else
      {
        strip.setPixelColor(i, 0);
      }
    }
    
    
    Serial.println("Update");
    uint8_t bitmap_size = sizeof(bitmap1);
    Serial.println(bitmap_size, HEX);
    uint8_t rand = random(currentState.SolidColorState.AnimationState.AnimationSpecificState.numOn);
    uint8_t val = 0;
    uint8_t next_val;
    if(currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep == 0)
    {
      if(rand == 0)
      {
        val = 0x1;
      }
      // update state
      for(uint8_t i = 0; i < bitmap_size; i++)
      {
        next_val = (bitmap1[i] & 0x80) >> 7;
        bitmap1[i] = (bitmap1[i] << 1) | val;
        val = next_val;
      }
    }
    
    rand = random(currentState.SolidColorState.AnimationState.AnimationSpecificState.numOff);
    val = 0;
    if(rand == 0)
    {
      val = 0x80;
    }
    
    for(uint8_t i = bitmap_size - 1; i < bitmap_size; i--)
    {
      next_val = (bitmap2[i] & 0x1) << 7;
      bitmap2[i] = (bitmap2[i] >> 1) | val;
      val = next_val;
    }
    Serial.println("End Update");
    
    currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep = (currentState.SolidColorState.AnimationState.AnimationSpecificState.currentStep+1) % currentState.SolidColorState.AnimationState.AnimationSpecificState.numSteps;
    
    strip.show();
  }
}
