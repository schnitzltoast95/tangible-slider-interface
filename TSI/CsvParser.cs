using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

public class QuestionnaireItem
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? LabelLeft { get; set; }
    public string? LabelRight { get; set; }
    public int ItemCount { get; set; }
    public double Threshold { get; set; }
}

public class CsvParser
{
    public static List<QuestionnaireItem> ParseCsv(string filePath)
    {
        var questionnaireItems = new List<QuestionnaireItem>();

        using (var reader = new StreamReader(filePath))
        {
            var headerLine = reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string?[] values = line.Split(';');

                if (values.Length < 6)
                {
                    MessageBox.Show(
                        "Invalid CSV file, row has less than 6 fields.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error
                    );
                    return null;
                }
                
                var item = new QuestionnaireItem
                {
                    Title = string.IsNullOrWhiteSpace(values[0]) ? null : values[0],
                    Description = string.IsNullOrWhiteSpace(values[1]) ? null : values[1],
                    LabelLeft = string.IsNullOrWhiteSpace(values[2]) ? null : values[2],
                    LabelRight = string.IsNullOrWhiteSpace(values[3]) ? null : values[3],
                    ItemCount = int.TryParse(values[4], out int itemCount) ? itemCount : 0,
                    Threshold = double.TryParse(values[5], out double threshold) ? threshold : 0.0
                };
                
                if (item.Threshold > 0.5) 
                    // TODO: demote to warning
                    MessageBox.Show(
                        "There are thresholds above 0,5 (50%) in your questionnaire. ", 
                        "THRESHOLDS BIGGER THAN 0,5", MessageBoxButton.OK, MessageBoxImage.Warning
                    );

                questionnaireItems.Add(item);
            }
        }

        return questionnaireItems;
    }
}

