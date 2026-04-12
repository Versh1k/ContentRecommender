using CsvHelper;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using System.Data;
using System.Globalization;

namespace MoodModelTrainer;

class Program
{
    // Класс для входных данных (должен совпадать с CSV)
    public class ModelInput
    {
        [LoadColumn(0)]
        public string Text { get; set; } = "";

        [LoadColumn(1)]
        public uint Label { get; set; } // uint обязательно!
    }

    // Класс для предсказаний
    public class ModelOutput
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }

        [ColumnName("Score")]
        public float[] Scores { get; set; } = Array.Empty<float>();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("=== Обучение модели настроения (Mood Analyzer) ===");

        // 1. Пути к файлам
        string dataPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender.Web\ML\Data\train_numeric.csv";
        string modelPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender.Web\ML\Models\MoodAnalyzer.zip";

        if (!File.Exists(dataPath))
        {
            Console.WriteLine($"❌ Файл данных не найден: {dataPath}");
            return;
        }

        // 2. Создаем MLContext
        var mlContext = new MLContext(seed: 42);

        // 3. Загружаем данные
        Console.WriteLine("Загрузка данных...");
        IDataView dataView = mlContext.Data.LoadFromTextFile<ModelInput>(
            path: dataPath,
            hasHeader: true,
            separatorChar: ',');

        // 4. Разделяем на train/test
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        var trainData = split.TrainSet;
        var testData = split.TestSet;

        // 5. Создаем пайплайн (ТОЛЬКО классические алгоритмы)
        Console.WriteLine("Построение пайплайна...");
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.Text)))
            .Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(
                binaryEstimator: mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 20,
                    numberOfTrees: 100,
                    minimumExampleCountPerLeaf: 10
                ),
                labelColumnName: "Label"
            ))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // 6. Обучаем модель
        Console.WriteLine("Обучение модели... Это займет несколько минут.");
        var model = pipeline.Fit(trainData);

        // 7. Оцениваем качество
        Console.WriteLine("Оценка модели...");
        var predictions = model.Transform(testData);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions);

        Console.WriteLine($"\n📊 Метрики модели:");
        Console.WriteLine($"  Микро-точность (MicroAccuracy): {metrics.MicroAccuracy:P2}");
        Console.WriteLine($"  Макро-точность (MacroAccuracy): {metrics.MacroAccuracy:P2}");
        Console.WriteLine($"  Логарифмическая потеря (LogLoss): {metrics.LogLoss:F4}");

        // Показываем матрицу ошибок (очень полезно!)
        Console.WriteLine("\n📉 Матрица ошибок (Confusion Matrix):");
        Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

        // 8. Сохраняем модель
        Console.WriteLine($"\nСохранение модели в {modelPath}...");
        mlContext.Model.Save(model, trainData.Schema, modelPath);
        Console.WriteLine($"✅ Модель успешно сохранена!");

        // 9. Тестовое предсказание
        Console.WriteLine("\n🔍 Тестовые предсказания:");
        TestPrediction(mlContext, model, "Этот фильм просто потрясающий! Лучшее, что я видел.");
        TestPrediction(mlContext, model, "Я очень напуган, это было страшно.");
        TestPrediction(mlContext, model, "Смешная комедия, посмеялся от души.");
        TestPrediction(mlContext, model, "Очень грустная история, плакал в конце.");
    }

    static void TestPrediction(MLContext mlContext, ITransformer model, string text)
    {
        var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
        var input = new ModelInput { Text = text };
        var prediction = predEngine.Predict(input);

        // Маппинг чисел обратно в текст
        string[] moodNames = { "aggression", "anxiety", "sarcasm", "positive", "neutral" };
        string predictedMood = prediction.PredictedLabel < moodNames.Length
            ? moodNames[prediction.PredictedLabel]
            : "unknown";

        Console.WriteLine($"  Текст: \"{text}\"");
        Console.WriteLine($"  Предсказание: {predictedMood} (label: {prediction.PredictedLabel})");
        if (prediction.Scores != null)
        {
            Console.WriteLine($"  Уверенность: {prediction.Scores.Max():P2}");
        }
        Console.WriteLine();
    }
}