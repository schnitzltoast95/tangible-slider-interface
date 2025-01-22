# Tangible Slider Interface

## Overview
The Tangible Slider Interface application allows for the evaluation and configuration of questionnaires using a scalable slider system.  
**Note:** This application is only available on **Windows**.

## Installation
1. Download the application and extract it to your preferred directory.
2. Move the entire folder to your user's home directory:
   - On Windows: `C:\Users\<YourUsername>`
3. We recommend placing the application folder and the CSV files (questionnaire and conditions file, if needed) together for easy access.

## Hardware Requirements
This application was tested on an **Arduino Nano** as the controller and the following hardware components:

| Component                  | Purpose                                       | Pin Assignment  |
|----------------------------|-----------------------------------------------|-----------------|
| **Button**                 | Detects user interactions                     | Digital Pin **2** |
| **Vibrationmotor**| Activated when slider reaches threshold value | Digital Pin **3** |
| **Sliding potentiometer**        | Reads current position                       | Analog Pin **A0** |
| **Serial Connection**      | For communication with the application       | Baudrate: **9600** |

Ensure all components are wired as described for proper functionality.

## CSV File Requirements
The first line of the CSV file is reserved for column headings and will be skipped during processing. The file must meet the following specifications:
- **Delimiter**: Semicolon (`;`)
- **Number of Columns**: 6 fields per row

### CSV Format
| Column          | Description                                |
|-----------------|--------------------------------------------|
| **1. Title**    | Title of the questionnaire item            |
| **2. Description**| Description or instructions                |
| **3. Left Label**| Label for the left side of the scale       |
| **4. Right Label**| Label for the right side of the scale      |
| **5. Questionnaire Items**| Number of questionnaire items          |
| **6. Threshold** | Threshold for validation (values higher than 0.5 are not supported)                  |

### Example (UEQ)
```csv
Title;Description;Left Label;Right Label;Questionnaire Items;Threshold
Attractiveness;How attractive is the product?;Unattractive;Attractive;7;0.15
Efficiency;How efficient is the product?;Inefficient;Efficient;7;0.15
```

## Usage
1. Run the application on your Windows system.
2. Connect and selected a tangible slider.
3. Upload your CSV file to the application.
4. Export collected data.

## License
This application is licensed under the [MIT License](LICENSE).
