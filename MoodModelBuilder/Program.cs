using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.ML.Transforms.Text;
using System.Text;

namespace MoodModelTrainer;

class Program
{
    public class ModelInput
    {
        [LoadColumn(0)]
        public string Text { get; set; } = "";
        [LoadColumn(1)]
        public uint Label { get; set; }
    }

    public class ModelOutput
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }
        [ColumnName("Score")]
        public float[] Scores { get; set; } = Array.Empty<float>();
    }
    static void Main(string[] args)
    {
        Console.WriteLine("Обучение модели\n");

        string dataPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender\ContentRecommender.Web\ML\Data";
        string trainPath = Path.Combine(dataPath, "train.csv");
        string valicPath = Path.Combine(dataPath, "valid.csv");
        string modelPath = Path.Combine(dataPath, @"..\Models\MoodAnalyzer.zip");

        if (!File.Exists(trainPath))
        {
            Console.WriteLine($"{trainPath} не найден"); return;
        }

        var mlContext = new MLContext(seed: 42);
        string[] moodNames =
        {
            "romantic", "mysterious", "sad", "funny", "crime", "military",
            "everyday", "tense", "horror", "inspiring", "epic", "adventure"
        };

        Console.WriteLine("Загрузка данных..");
        var trainData = mlContext.Data.LoadFromTextFile<ModelInput>
            (trainPath, hasHeader: true, separatorChar: '|', allowQuoting: true, trimWhitespace: true);

        var counts = new int[12];
        var trainRows = mlContext.Data.CreateEnumerable<ModelInput>
            (trainData, false).ToList();
        foreach (var row in trainRows) if (row.Label < 12) counts[row.Label]++;

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.Text)))
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 15,
                numberOfIterations: 100,
                learningRate: 0.1f,
                minimumExampleCountPerLeaf: 50))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        Console.WriteLine("\n Кросс-валидация:");
        var cvResults = mlContext.MulticlassClassification.CrossValidate(
            trainData, pipeline, numberOfFolds: 5, labelColumnName: "Label");

        var avgMicro = cvResults.Average(r => r.Metrics.MicroAccuracy);
        var avgMacro = cvResults.Average(r => r.Metrics.MacroAccuracy);
        var avgLogLoss = cvResults.Average(r => r.Metrics.LogLoss);

        Console.WriteLine($"Микро-точность: {avgMicro:P2}");
        Console.WriteLine($"Макро-точность: {avgMacro:P2}");
        Console.WriteLine($"Потери: {avgLogLoss:F4}");

        Console.WriteLine("\n Обучение модели");
        var model = pipeline.Fit(trainData);

        if (File.Exists(valicPath))
        {
            Console.WriteLine("\n Результаты на валидации");
            var validData = mlContext.Data.LoadFromTextFile<ModelInput>(
                valicPath, hasHeader: true, separatorChar: '|', allowQuoting: true, trimWhitespace: true);
            var predictions = model.Transform(validData);
            var metrics = mlContext.MulticlassClassification.Evaluate(
                predictions, labelColumnName: "Label");

            Console.WriteLine($"\n Микро-точность: {metrics.MicroAccuracy:P2}");
            Console.WriteLine($"Макро-точность: {metrics.MacroAccuracy:P2}");
            Console.WriteLine($"Потери: {metrics.LogLoss:F4}");

            var top3Accuracy = CalculateTopAccuracy(mlContext, model, validData, 3);
            Console.WriteLine($" Топ-3 точность: {top3Accuracy:P2}");
        }

        Console.WriteLine("Сохранение модели..");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        mlContext.Model.Save(model, trainData.Schema, modelPath);

        Console.WriteLine("Тестирование модели:");
        TestPredictionsWithConfidence(mlContext, model, moodNames);
    }

    static double CalculateTopAccuracy(MLContext mLContext, ITransformer model, IDataView data, int k)
    {
        var predictions = model.Transform(data);
        var rows = mLContext.Data.CreateEnumerable<ModelOutput>(predictions, false).ToList();
        var labels = mLContext.Data.CreateEnumerable<ModelInput>(data, false).Select(r => r.Label).ToList();

        int correct = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var scores = rows[i].Scores;

            if (scores == null || scores.Length == 0)
            {
                continue;
            }

            var topK = scores.Select((s, idx) => new { Score = s, Index = idx })
                .OrderByDescending(x => x.Score)
                .Take(k)
                .Select(x => x.Index)
                .ToList();

            if (topK.Contains((int)labels[i]))
            {
                correct++;
            }
        }
        return (double)correct / Math.Max(1, rows.Count);
    }

    static void TestPredictionsWithConfidence(MLContext mLContext, ITransformer model, string[] moodNames)
    {
        var predEngine = mLContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(
            model, ignoreMissingColumns: true);

        var tests = new[]
        {
            ("хочу романтику и любовь", 0),
            ("детектив с тайной", 1),
            ("грустная драма", 2),
            ("смешная комедия", 3),
            ("криминальный триллер", 4),
            ("военный фильм", 5),
            ("спокойный вечер", 6),
            ("напряженный триллер", 7),
            ("страшный ужастик", 8),
            ("вдохновляющая история", 9),
            ("эпичное фэнтези с драконами", 10),
            ("приключения в джунглях", 11),
        };
        
        foreach (var (text, expected) in tests)
        {
            var pred = predEngine.Predict(new ModelInput { Text = text });
            var confidence = pred.Scores?.Length > 0 ? pred.Scores[pred.PredictedLabel] : 0f;

            var top2 = pred.Scores?.Select((s, i) => new { Mood = moodNames[i], Score = s})
                .OrderByDescending(x => x.Score)
                .Take(2)
                .ToList();
            var status = pred.PredictedLabel == expected ? "да" : "нет";

            Console.WriteLine($"{status} \"{text}\"");
            Console.WriteLine($"{moodNames[pred.PredictedLabel]} (уверенность: {confidence:P2})");
            if (top2 != null)
            {
                Console.WriteLine($"Топ-2: {top2[0].Mood} ({top2[0].Score:P2}), {top2[1].Mood} ({top2[1].Score:P2})");
            }
        }
    }
}