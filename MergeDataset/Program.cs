using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace MergeDataset;

class Program
{
    // Словарь для преобразования текстовых меток в числа
    private static readonly Dictionary<string, int> LabelToInt = new()
    {
        ["aggression"] = 0,
        ["anxiety"] = 1,
        ["sarcasm"] = 2,
        ["positive"] = 3,
        ["neutral"] = 4
    };

    static void Main(string[] args)
    {
        Console.WriteLine("=== УНИВЕРСАЛЬНЫЙ ОБРАБОТЧИК ДАТАСЕТА ===\n");

        string dataPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender.Web\ML\Data";

        // ШАГ 1: Объединяем train_part файлы в один train.csv
        Console.WriteLine("ШАГ 1: Объединение train файлов...");
        MergeTrainFiles(dataPath);

        // ШАГ 2: Нормализуем train.csv (преобразуем метки в числа)
        Console.WriteLine("\nШАГ 2: Нормализация train.csv...");
        NormalizeFile(Path.Combine(dataPath, "train.csv"), Path.Combine(dataPath, "train_numeric.csv"));

        // ШАГ 3: Нормализуем valid.csv
        Console.WriteLine("\nШАГ 3: Нормализация valid.csv...");
        NormalizeFile(Path.Combine(dataPath, "valid.csv"), Path.Combine(dataPath, "valid_numeric.csv"));

        // ШАГ 4: Показываем статистику
        Console.WriteLine("\nШАГ 4: Статистика готовых файлов:");
        ShowStats(Path.Combine(dataPath, "train_numeric.csv"), "train_numeric.csv");
        ShowStats(Path.Combine(dataPath, "valid_numeric.csv"), "valid_numeric.csv");

        Console.WriteLine("\n✅ ВСЕ ГОТОВО! Файлы для обучения:");
        Console.WriteLine($"  - {Path.Combine(dataPath, "train_numeric.csv")}");
        Console.WriteLine($"  - {Path.Combine(dataPath, "valid_numeric.csv")}");
    }

    static void MergeTrainFiles(string dataPath)
    {
        var trainFiles = Directory.GetFiles(dataPath, "train_part_*.csv").OrderBy(f => f).ToList();
        Console.WriteLine($"Найдено файлов: {trainFiles.Count}");

        if (trainFiles.Count == 0)
        {
            Console.WriteLine("❌ Файлы train_part_*.csv не найдены!");
            return;
        }

        var allLines = new List<string>();
        bool headerAdded = false;

        foreach (var file in trainFiles)
        {
            Console.Write($"Читаем {Path.GetFileName(file)}... ");
            var lines = File.ReadAllLines(file, Encoding.UTF8);

            if (!headerAdded)
            {
                allLines.Add(lines[0]); // добавляем заголовок только один раз
                headerAdded = true;
                allLines.AddRange(lines.Skip(1));
            }
            else
            {
                allLines.AddRange(lines.Skip(1));
            }

            Console.WriteLine($"{lines.Length - 1} записей");
        }

        string outputPath = Path.Combine(dataPath, "train.csv");
        File.WriteAllLines(outputPath, allLines, Encoding.UTF8);
        Console.WriteLine($"✅ Создан {outputPath} - всего {allLines.Count - 1} записей");
    }

    static void NormalizeFile(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"❌ Файл {inputPath} не найден!");
            return;
        }

        var lines = File.ReadAllLines(inputPath, Encoding.UTF8);
        Console.WriteLine($"Всего строк: {lines.Length}");

        var outputLines = new List<string>();
        outputLines.Add("Text,Label"); // новый заголовок с числовыми метками

        int converted = 0;
        int errors = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Находим последнюю запятую (разделитель между текстом и меткой)
            var lastCommaIndex = line.LastIndexOf(',');
            if (lastCommaIndex < 0) continue;

            var text = line.Substring(0, lastCommaIndex).Trim();
            var label = line.Substring(lastCommaIndex + 1).Trim().ToLowerInvariant();

            // Очищаем от кавычек
            text = text.Trim('"');
            label = label.Trim('"');

            // Экранируем кавычки в тексте для CSV
            text = text.Replace("\"", "\"\"");

            if (LabelToInt.TryGetValue(label, out int numericLabel))
            {
                outputLines.Add($"\"{text}\",{numericLabel}");
                converted++;
            }
            else
            {
                // Если метка неизвестна, ставим neutral (4)
                Console.WriteLine($"  ⚠️ Неизвестная метка '{label}' в строке {i}, ставим neutral (4)");
                outputLines.Add($"\"{text}\",4");
                errors++;
            }
        }

        File.WriteAllLines(outputPath, outputLines, Encoding.UTF8);

        Console.WriteLine($"  Преобразовано: {converted} записей");
        Console.WriteLine($"  Заменено на neutral: {errors} записей");
        Console.WriteLine($"  Сохранено в: {Path.GetFileName(outputPath)}");
    }

    static void ShowStats(string filePath, string fileName)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"  {fileName}: ❌ ФАЙЛ НЕ НАЙДЕН");
            return;
        }

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        var labelCounts = new Dictionary<int, int>();

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length >= 2)
            {
                var labelStr = parts.Last().Trim();
                if (int.TryParse(labelStr, out int label))
                {
                    if (!labelCounts.ContainsKey(label))
                        labelCounts[label] = 0;
                    labelCounts[label]++;
                }
            }
        }

        Console.WriteLine($"  {fileName}: {lines.Length - 1} записей");
        foreach (var kv in labelCounts.OrderBy(k => k.Key))
        {
            string labelName = LabelToInt.FirstOrDefault(x => x.Value == kv.Key).Key ?? "unknown";
            Console.WriteLine($"    {kv.Key} ({labelName}): {kv.Value}");
        }
    }
}