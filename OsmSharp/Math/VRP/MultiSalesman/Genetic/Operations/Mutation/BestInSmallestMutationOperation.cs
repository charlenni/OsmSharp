﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Math.AI.Genetic;
using OsmSharp.Math.AI.Genetic.Operations;
using OsmSharp.Math.AI.Genetic.Solvers;
using OsmSharp.Math.VRP.MultiSalesman.Genetic;
using OsmSharp.Math.VRP.MultiSalesman.Genetic.Helpers;

namespace OsmSharp.Math.VRP.MultiSalesman.Solver.Operations.Mutation
{
    internal class BestInSmallestMutationOperation : IMutationOperation<List<Genome>, Problem, Fitness>
    {

        public string Name
        {
            get
            {
                return "NotSet";
            }
        }

        public Individual<List<Genome>, Problem, Fitness> Mutate(
            Solver<List<Genome>, Problem, Fitness> solver, Individual<List<Genome>, Problem, Fitness> mutating)
        {
            //if (mutating.Genomes.Count < 2)
            //{
            //    return mutating;
            //}

            //// get from the largest round; place in the smallest round.
            //if (!mutating.FitnessCalculated)
            //{
            //    mutating.CalculateFitness(solver.Problem, solver.FitnessCalculator);
            //}
            //Genome smallest = IndividualHelper.GetSmallest(mutating);
            //int smallest_idx = mutating.Genomes.IndexOf(smallest);
            
            //// build a list of the rest of the cities.
            //List<int> cities = new List<int>();
            //for (int city_to_place = 0; city_to_place < solver.Problem.Cities; city_to_place++)
            //{
            //    if (!smallest.Contains(city_to_place))
            //    {
            //        cities.Add(city_to_place);
            //    }
            //}

            //// best place one of the largest cities into the smallest.
            //Tools.Math.VRP.MultiSalesman.Genetic.Helpers.BestPlacementHelper.BestPlacementResult result =
            //    BestPlacementHelper.CalculateBestPlacementInGenome(
            //    solver.Problem, (solver.FitnessCalculator as FitnessCalculator), smallest, cities);

            //// remove from largest/place in smallest after copying.
            //Individual<List<Genome>, Problem, Fitness> copy = mutating.Copy();
            //smallest = copy.Genomes[smallest_idx];
            //foreach (Genome genome in copy.Genomes)
            //{
            //    if (genome.Remove(result.City))
            //    {
            //        if (genome.Count == 0)
            //        {
            //            genome.Add(result.City);
            //        }
            //        else
            //        {
            //            IndividualHelper.PlaceInGenome(smallest, result.CityIdx, result.City);
            //        }
            //        break;
            //    }
            //}

            //// recalculate fitness.
            ////copy.CalculateFitness(solver.Problem, solver.FitnessCalculator);

            //copy.Validate(solver.Problem);

            //return copy;
            throw new NotImplementedException();
        }
    }
}
