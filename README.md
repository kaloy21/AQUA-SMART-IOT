

AquaSmart is an automated, Internet of Things (IoT)-enabled water quality monitoring and control system. Powered by an ESP32 microcontroller, the system continuously tracks essential environmental parameters of a water body and takes real-time corrective actions using an array of relays. Additionally, it features a smart on-boarding Wi-Fi captive portal and syncs data seamlessly to a cloud database for remote monitoring.

Core Features & Functionalities

1. Multi-Sensor Water Quality Monitoring
   The system monitors four critical water health parameters in real-time:
    - pH Levels: Uses an analog pH sensor to calculate precise acidity/alkalinity via a calibrated linear formula ($M = -6.0$, $B = 15.274$). It takes an average of multiple readings and sorts them to eliminate telemetry noise.
    - Turbidity (Water Clarity): Measures suspended particles in the water using an analog turbidity sensor, mapping the raw data into a clean 0% to 100% clarity percentage and defining status thresholds (CLEAR, CLOUDY, or DIRTY).
    - Dissolved Oxygen (DO): Measures oxygen saturation ($mg/L$) using an analog DO sensor. The code utilizes a built-in freshwater compensation formula based on real-time temperature to ensure precise saturation scaling.
    - Water Temperature: Leverages a DS18B20 1-wire digital temperature sensor to capture high-precision readings in both Celsius and Fahrenheit.
  
2. Automated Smart Control (Relay System)
   AquaSmart doesn't just monitor; it actively manages the ecosystem using four physical relays based on conditional thresholds:
   - Circulation Pumps (Relay 1 & 2): Triggered to turn ON if water temperature spikes ($\ge 32^\circ\text{C}$) or if the pH drops into dangerous zones ($\text{pH} < 6.5$ or $> 8.5$) to safely circulate and aerate the water.
   - Heater (TEMP_RELAY): Automatically activates if the water temperature drops below a chilly $26^\circ\text{C}$ and turns off once it safely exceeds $32^\circ\text{C}$.
   - Turbidity / Filter Pump (TURBIDITY_RELAY): Actively manages water clarity and oxygen. It switches on if turbidity climbs above 50% (to filter out debris) or if Dissolved Oxygen levels spike abnormally high ($\ge 6\text{ mg/L}$).
  
3. Smart Wi-Fi Provisioning (Captive Portal)
   To make deploying the device easy for non-technical users, AquaSmart includes an automated Wi-Fi manager.
   - If the device cannot find a saved local network on boot, it automatically launches its own local Wi-Fi Hotspot named "AquaSmart Setup" and fires up a DNS Captive Portal.
   - Users connecting to the hotspot are directed to a stylized HTML webpage where they can securely input their local Wi-Fi credentials. The system saves these onto the ESP32's non-volatile Preferences storage and reboots to connect.
  
4. Cloud Integration & Local UI
   - Firebase Realtime Database: Once connected to the internet, AquaSmart securely streams all data points—including sensor values, calculated metrics, and the current operational states of the pumps and heater—to Google Firebase using an API key.
   - Local LCD Interface: For on-site management, a $20\times4$ character I2C LCD screen outputs crisp, real-time lines displaying current Temperature, pH, Turbidity percentage (with status strings), and Dissolved Oxygen levels.
  
Hardware Component Architecture
Based on the firmware configuration, the system relies on the following hardware:
   - Microcontroller: ESP32 (utilizing built-in Wi-Fi, NVS Preferences, and analog-to-digital converters).
   - Display: $20\times4$ I2C LCD Display (Address 0x27).
   - Sensors: DS18B20 Temperature Probe, Analog pH Sensor, Analog Turbidity Sensor, and an Analog Dissolved Oxygen Probe.
   - Actuators: 4-Channel Relay module managing a Heater, Filter Pump, and dual Water Circulation Pumps.

Typical Use Cases
Given its automated nature and cloud connectivity, AquaSmart is ideally designed for:
   - Aquaculture & Fish Farming: Ensuring stable oxygen, temperature, and pH environments for aquatic life.
   - Hydroponics: Maintaining optimal nutrient-water pH balances and pump circulation.
   - Smart Aquarium Management: Giving hobbyists automated tank maintenance and remote metrics viewable from anywhere via Firebase.
   
