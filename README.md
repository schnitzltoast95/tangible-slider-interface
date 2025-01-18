# Tangible Slider Interface

## Overview
The Tangible Slider Interface application allows for the evaluation and configuration of questionnaires using a scalable slider system. Data is imported through a CSV file that must adhere to a specific format.

## Installation
1. Download the application and extract it to your preferred directory.
2. Move the entire folder to your user's home directory:
   - On Windows: `C:\Users\<YourUsername>`
   - On Linux/macOS: `/home/<YourUsername>`
3. We recommend using a Folder to store the application as well as used questionnaire and conditions file (if needed).

## CSV File Requirements
The CSV file must meet the following specifications:
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
1. Launch the application by opening the `TSI.sln` file (for developers) or the compiled `.exe` file in the `bin` folder (for end-users).
2. Connect and selected a tangible slider.
3. Upload your CSV file to the application.
4. Export collected data.

## License
This application is licensed under the [MIT License](LICENSE).