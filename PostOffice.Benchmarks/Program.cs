using BenchmarkDotNet.Running;

namespace PostOffice.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<PostOfficeBenchmarks>();
        Console.WriteLine(summary);
    }
}
