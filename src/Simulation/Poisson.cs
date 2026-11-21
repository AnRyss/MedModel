using System;
using SimSharp;

public class Poisson : Distribution<int> {
    private readonly double _lambda;
    

    public Poisson(double lambda) {
        if (lambda <= 0) throw new ArgumentException("Lambda must be > 0", nameof(lambda));
        _lambda = lambda;
    }

    public override int Sample(IRandom random) => Sample(random, _lambda);

   
    public static int Sample(IRandom random, double lambda) {
    
        if (lambda > 20.0) {
            
            double sample = new Uniform(0,1).Sample(random) * Math.Sqrt(lambda) + lambda;
            return Math.Max(0, (int)Math.Round(sample));
        }

        
        double L = Math.Exp(-lambda);
        double p = 1.0;
        int k = 0;

        do {
            k++;
            p *= random.NextDouble();
        } while (p > L);

        return k - 1;
    }
}
