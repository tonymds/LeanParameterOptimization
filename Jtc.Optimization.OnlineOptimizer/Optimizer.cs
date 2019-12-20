﻿using Jtc.Optimization.Objects;
using Jtc.Optimization.Objects.Interfaces;
using Jtc.Optimization.Transformation;
using SharpLearning.Optimization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jtc.Optimization.OnlineOptimizer
{

    public abstract class Optimizer
    {

        private bool IsMaximizing { get; set; }
        protected ActivityLogger ActivityLogger { get; set; }


        public async Task<IterationResult> Start(IOptimizerConfiguration config, ActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;

            var parameters = config.Genes.Select(s =>
                new MinMaxParameterSpec(min: (double)(s.MinDecimal ?? s.MinInt.Value), max: (double)(s.MaxDecimal ?? s.MaxInt.Value),
                transform: Transform.Linear, parameterType: s.Precision > 0 ? ParameterType.Continuous : ParameterType.Discrete)
                ).ToArray();

            IOptimizer optimizerMethod = null;
            if (config.Fitness != null)
            {
                if (config.Fitness.OptimizerTypeName == Enums.OptimizerTypeOptions.RandomSearch.ToString())
                {
                    optimizerMethod = new RandomSearchOptimizer(parameters, iterations: config.Generations, seed: 42, runParallel: false);
                }
                else if (config.Fitness.OptimizerTypeName == Enums.OptimizerTypeOptions.ParticleSwarm.ToString())
                {
                    optimizerMethod = new ParticleSwarmOptimizer(parameters, maxIterations: config.Generations, numberOfParticles: config.PopulationSize,
                        seed: 42, maxDegreeOfParallelism: 1);
                }
                else if (config.Fitness.OptimizerTypeName == Enums.OptimizerTypeOptions.Bayesian.ToString())
                {
                    optimizerMethod = new BayesianOptimizer(parameters: parameters, iterations: config.Generations, randomStartingPointCount: config.PopulationSize,
                        functionEvaluationsPerIterationCount: config.PopulationSize, seed: 42);
                }
                else if (config.Fitness.OptimizerTypeName == Enums.OptimizerTypeOptions.GlobalizedBoundedNelderMead.ToString())
                {
                    optimizerMethod = new GlobalizedBoundedNelderMeadOptimizer(parameters, maxRestarts: config.Generations,
                        maxIterationsPrRestart: config.PopulationSize, seed: 42, maxDegreeOfParallelism: 1);
                }
                else if (config.Fitness.OptimizerTypeName == Enums.OptimizerTypeOptions.GridSearch.ToString())
                {
                    optimizerMethod = new GridSearchOptimizer(parameters, runParallel: false);
                }
            }

            var result = await Task.Run(() => optimizerMethod.OptimizeBest(Minimize));

            //var result = optimizerMethod.OptimizeBest(Minimize);

            //Console.WriteLine("Error: " + result.Error.ToString("N"));

            return new IterationResult { ParameterSet = result.ParameterSet, Cost = IsMaximizing ? result.Error * -1 : result.Error };
        }

        public abstract Task<OptimizerResult> Minimize(double[] parameters);

    }
}