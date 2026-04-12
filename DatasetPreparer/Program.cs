using System.Globalization;
using System.Text;

namespace DatasetPreparer;

class Program
{
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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.WriteLine("=== ПОДГОТОВКА ДАТАСЕТА ===\n");

        string dataPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender.Web\ML\Data";

        // ШАГ 1: Объединяем train_part файлы
        Console.WriteLine("ШАГ 1: Объединение train файлов...");
        var trainPath = MergeTrainFiles(dataPath);
        if (trainPath == null) return;

        // ШАГ 2: Конвертируем train в числовой формат
        Console.WriteLine("\nШАГ 2: Конвертация train.csv в числовой формат...");
        string trainFinalPath = Path.Combine(dataPath, "train_final.csv");
        ConvertToNumeric(trainPath, trainFinalPath, Encoding.UTF8);

        // ШАГ 3: Конвертируем valid в числовой формат (тоже UTF-8)
        Console.WriteLine("\nШАГ 3: Конвертация valid.csv в числовой формат...");
        string validSourcePath = Path.Combine(dataPath, "valid.csv");
        string validFinalPath = Path.Combine(dataPath, "valid_final.csv");
        ConvertToNumeric(validSourcePath, validFinalPath, Encoding.UTF8); // ← UTF-8!

        // ШАГ 4: Проверяем результаты
        Console.WriteLine("\nШАГ 4: Проверка созданных файлов...");
        VerifyFile(trainFinalPath, "TRAIN");
        VerifyFile(validFinalPath, "VALID");

        Console.WriteLine("\n✅ ПОДГОТОВКА ЗАВЕРШЕНА!");
    }

    static string? MergeTrainFiles(string dataPath)
    {
        var trainFiles = Directory.GetFiles(dataPath, "train_part_*.csv").OrderBy(f => f).ToList();
        if (trainFiles.Count == 0) return null;

        Console.WriteLine($"Найдено файлов: {trainFiles.Count}");
        var allLines = new List<string>();
        bool headerAdded = false;

        foreach (var file in trainFiles)
        {
            var lines = File.ReadAllLines(file, Encoding.UTF8);
            if (!headerAdded)
            {
                allLines.Add(lines[0]);
                headerAdded = true;
                allLines.AddRange(lines.Skip(1));
            }
            else
            {
                allLines.AddRange(lines.Skip(1));
            }
            Console.WriteLine($"  {Path.GetFileName(file)}: {lines.Length - 1} записей");
        }

        string trainPath = Path.Combine(dataPath, "train.csv");
        File.WriteAllLines(trainPath, allLines, Encoding.UTF8);
        Console.WriteLine($"✅ Создан train.csv - всего {allLines.Count - 1} записей");
        return trainPath;
    }

    static void ConvertToNumeric(string inputPath, string outputPath, Encoding fileEncoding)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"❌ Файл {inputPath} не найден!");
            return;
        }

        var lines = File.ReadAllLines(inputPath, fileEncoding);
        Console.WriteLine($"Читаем {Path.GetFileName(inputPath)}: {lines.Length} строк (кодировка: {fileEncoding.WebName})");

        var outputLines = new List<string> { "Text,Label" };
        int converted = 0, errors = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var lastComma = line.LastIndexOf(',');
            if (lastComma < 0) { errors++; continue; }

            var text = line.Substring(0, lastComma).Trim().Trim('"');
            var label = line.Substring(lastComma + 1).Trim().Trim('"');

            text = text.Replace("\"", "\"\"");

            if (int.TryParse(label, out int num) && num >= 0 && num <= 4)
            {
                outputLines.Add($"\"{text}\",{num}");
                converted++;
            }
            else if (LabelToInt.TryGetValue(label.ToLowerInvariant(), out int mapped))
            {
                outputLines.Add($"\"{text}\",{mapped}");
                converted++;
            }
            else
            {
                outputLines.Add($"\"{text}\",4");
                errors++;
            }
        }

        File.WriteAllLines(outputPath, outputLines, Encoding.UTF8);
        Console.WriteLine($"✅ Создан {Path.GetFileName(outputPath)}: {converted} записей, {errors} замен");
    }

    static void VerifyFile(string filePath, string name)
    {
        if (!File.Exists(filePath)) return;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        Console.WriteLine($"\n📊 Проверка {name}: {lines.Length - 1} записей");

        for (int i = 1; i < Math.Min(4, lines.Length); i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length >= 2)
            {
                var preview = parts[0].Length > 50 ? parts[0].Substring(0, 47) + "..." : parts[0];
                Console.WriteLine($"   {i}: {preview} | {parts.Last()}");
            }
        }
        Console.WriteLine($"   ✅ Все метки — числа");
    }
}