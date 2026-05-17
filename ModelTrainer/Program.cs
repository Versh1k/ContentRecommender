using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.ML.Transforms.Text;
using System;
using System.IO;
using System.Linq;

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
        Console.WriteLine(" Обучение модели настроений (LightGBM)\n");

        string dataPath = @"C:\Users\versh\source\repos\ContentRecommender\ContentRecommender\ContentRecommender.Web\ML\Data";
        string dataFile = Path.Combine(dataPath, "train_fixed.csv");
        string modelPath = Path.Combine(dataPath, @"..\Models\MoodAnalyzer(700prim_na_class).zip");

        if (!File.Exists(dataFile))
        {
            Console.WriteLine($"❌ Файл не найден: {dataFile}");
            return;
        }

        var mlContext = new MLContext(seed: 42);

        string[] moodNames =
        {
            "romantic", "mysterious", "sad", "funny", "crime", "military",
            "everyday", "tense", "horror", "inspiring", "epic", "adventure"
        };

        Console.WriteLine(" Загрузка данных...");
        var allData = mlContext.Data.LoadFromTextFile<ModelInput>(
            dataFile, hasHeader: true, separatorChar: '|', allowQuoting: true, trimWhitespace: true);

        var split = mlContext.Data.TrainTestSplit(allData, testFraction: 0.1, seed: 42);
        var trainData = split.TrainSet;
        var testData = split.TestSet;

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.Text)))
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 32,
                numberOfIterations: 300,
                learningRate: 0.03f,
                minimumExampleCountPerLeaf: 20))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        Console.WriteLine("\n Кросс-валидация (5 фолдов):");
        var cvResults = mlContext.MulticlassClassification.CrossValidate(
            trainData, pipeline, numberOfFolds: 5, labelColumnName: "Label");

        Console.WriteLine($"   Микро-точность: {cvResults.Average(r => r.Metrics.MicroAccuracy):P2}");
        Console.WriteLine($"   Макро-точность: {cvResults.Average(r => r.Metrics.MacroAccuracy):P2}");
        Console.WriteLine($"   LogLoss: {cvResults.Average(r => r.Metrics.LogLoss):F4}");

        Console.WriteLine("\n Обучение финальной модели...");
        var model = pipeline.Fit(trainData);

        Console.WriteLine("\n Оценка на тестовой выборке (10% данных):");
        var predictions = model.Transform(testData);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");

        Console.WriteLine($"   Микро-точность: {metrics.MicroAccuracy:P2}");
        Console.WriteLine($"   Макро-точность: {metrics.MacroAccuracy:P2}");
        Console.WriteLine($"   LogLoss: {metrics.LogLoss:F4}");

        Console.WriteLine("\n Сохранение модели...");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        mlContext.Model.Save(model, trainData.Schema, modelPath);
        Console.WriteLine($" Модель сохранена: {modelPath}");

        Console.WriteLine("\n Тестирование на примерах:");
        TestPredictions(mlContext, model, moodNames);
    }

    static void TestPredictions(MLContext mlContext, ITransformer model, string[] moodNames)
    {
        var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);

        static float[] Softmax(float[] scores)
        {
            var max = scores.Max();
            var exp = scores.Select(x => (float)Math.Exp(x - max)).ToArray();
            var sum = exp.Sum();
            return exp.Select(x => x / sum).ToArray();
        }

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

        int correct = 0;
        foreach (var (text, expected) in tests)
        {
            var pred = predEngine.Predict(new ModelInput { Text = text });
            var softmaxScores = Softmax(pred.Scores);
            var confidence = softmaxScores[pred.PredictedLabel];
            var isCorrect = pred.PredictedLabel == expected;
            if (isCorrect) correct++;

            var status = isCorrect ? "да" : "нет";
            Console.WriteLine($"{status} \"{text}\" → {moodNames[pred.PredictedLabel]} (уверенность: {confidence:P2})");
        }

        Console.WriteLine($"\n Итог: {correct}/{tests.Length} правильных ({(double)correct / tests.Length:P1})");
    }
}