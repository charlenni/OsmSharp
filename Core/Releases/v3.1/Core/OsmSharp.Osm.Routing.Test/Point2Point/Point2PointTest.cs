﻿// OsmSharp - OpenStreetMap tools & library.
// Copyright (C) 2012 Abelshausen Ben
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
using System.IO;
using OsmSharp.Tools.Math.Geo;
using System.Data;
using OsmSharp.Osm.Data;
using OsmSharp.Osm.Xml;
using OsmSharp.Tools.Xml.Sources;
using OsmSharp.Osm.Data.Raw.XML.OsmSource;
using OsmSharp.Routing;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Router;
using OsmSharp.Routing.Graph;
using OsmSharp.Osm;
using OsmSharp.Routing.Osm.Interpreter;
using System.Reflection;
using System.Diagnostics;
using OsmSharp.Routing.Graph.DynamicGraph;
using OsmSharp.Routing.Route;
using OsmSharp.Routing.Graph.Router;

namespace OsmSharp.Routing.Osm.Test.Point2Point
{
    internal abstract class Point2PointTest<EdgeData> 
        where EdgeData : IDynamicGraphEdgeData
    {
        /// <summary>
        /// Executes some general random query performance evaluation(s).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="test_count"></param>
        public void ExecuteTest(string name, int test_count)
        {
            string xml_embedded = string.Format("OsmSharp.Routing.Osm.Test.TestData.{0}.osm", name);

            this.ExecuteTest(name, Assembly.GetExecutingAssembly().GetManifestResourceStream(xml_embedded), false, test_count);
        }

        /// <summary>
        /// Executes some general random query performance evaluation(s).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data_stream"></param>
        /// <param name="pbf"></param>
        /// <param name="test_count"></param>
        public void ExecuteTest(string name, Stream data_stream, bool pbf, int test_count)
        {
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine("Test {0} -> {1}x", name, test_count);

            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();
            IBasicRouterDataSource<EdgeData> data = this.BuildData(data_stream, pbf,
                interpreter, null);

            // build router;
            IBasicRouter<EdgeData> basic_router = this.BuildBasicRouter(data);
            IRouter<RouterPoint> router = this.BuildRouter(data, interpreter, basic_router);

            // generate random route pairs.
            List<KeyValuePair<RouterPoint, RouterPoint>> test_pairs =
                new List<KeyValuePair<RouterPoint, RouterPoint>>(test_count);
            while (test_pairs.Count < test_count)
            {
                uint first = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                    (int)data.VertexCount - 1) + 1;
                uint second = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                    (int)data.VertexCount - 1) + 1;

                float latitude_first, longitude_first;
                data.GetVertex(first, out latitude_first, out longitude_first);
                RouterPoint first_resolved = router.Resolve(VehicleEnum.Car, 
                    new GeoCoordinate(latitude_first, longitude_first));

                float latitude_second, longitude_second;
                data.GetVertex(second, out latitude_second, out longitude_second);
                RouterPoint second_resolved = router.Resolve(VehicleEnum.Car, 
                    new GeoCoordinate(latitude_second, longitude_second));


                if (((second_resolved != null) &&
                    (first_resolved != null)) &&
                    (router.CheckConnectivity(VehicleEnum.Car, first_resolved, 30) &&
                    router.CheckConnectivity(VehicleEnum.Car, second_resolved, 30)))
                {
                    test_pairs.Add(new KeyValuePair<RouterPoint, RouterPoint>(
                        first_resolved, second_resolved));
                }

                OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(test_pairs.Count, test_count, "Osm.Routing.Test.Point2Point.Point2PointTest<EdgeData>.Execute",
                    "Building pairs list...");
            }
            //Console.ReadLine();

            int successes = 0;
            long before = DateTime.Now.Ticks;
            foreach (KeyValuePair<RouterPoint, RouterPoint> pair in test_pairs)
            {
                //OsmSharp.Routing.Route.OsmSharpRoute route = router.Calculate(VehicleEnum.Car, pair.Key, pair.Value);
                double route_weight = router.CalculateWeight(VehicleEnum.Car, pair.Key, pair.Value);
                if (route_weight < float.MaxValue)
                {
                    successes++;
                }
            }
            long after = DateTime.Now.Ticks;
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine();
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine(string.Format("Average calculation time for {0} random routes: {1}ms with {2} successes",
                test_count, (new TimeSpan((after - before) / test_count)).TotalMilliseconds.ToString(), successes));
        }

        /// <summary>
        /// Executes some general random query performance evaluation(s).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="test_count"></param>
        public void ExecuteTestIncrementalBoundingBox(string name, int test_count, GeoCoordinateBox outer_box)
        {
            string xml_embedded = string.Format("OsmSharp.Routing.Osm.Test.TestData.{0}.osm", name);

            this.ExecuteTestIncrementalBoundingBox(name, Assembly.GetExecutingAssembly().GetManifestResourceStream(xml_embedded), false, test_count,
                outer_box);
        }

        /// <summary>
        /// Executes a test using a incrementing bounding box.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data_stream"></param>
        /// <param name="pbf"></param>
        /// <param name="test_count"></param>
        /// <param name="outer_box"></param>
        public void ExecuteTestIncrementalBoundingBox(string name, Stream data_stream, bool pbf, int test_count, GeoCoordinateBox outer_box)
        {
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine("Incremental Test {0} -> {1}x", name, test_count);

            int box_count = 20;
            for (int idx = 1; idx < box_count; idx++)
            {
                GeoCoordinateBox box = new GeoCoordinateBox(
                        new GeoCoordinate(
                    outer_box.Center.Latitude - ((outer_box.DeltaLat / (float)box_count) * (float)idx),
                    outer_box.Center.Longitude - ((outer_box.DeltaLon / (float)box_count) * (float)idx)),
                        new GeoCoordinate(
                    outer_box.Center.Latitude + ((outer_box.DeltaLat / (float)box_count) * (float)idx),
                    outer_box.Center.Longitude + ((outer_box.DeltaLon / (float)box_count) * (float)idx)));

                OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();
                OsmSharp.Tools.Output.OutputStreamHost.WriteLine("Testing for a box with total surface of {0}m²",
                    box.Corners[0].DistanceReal(box.Corners[1]).Value * box.Corners[0].DistanceReal(box.Corners[2]).Value);
                IBasicRouterDataSource<EdgeData> data = this.BuildData(data_stream, pbf,
                    interpreter, box);

                this.DoExecuteTests(data, interpreter, test_count);
            }
        }

        /// <summary>
        /// Executes the actual tests.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="interpreter"></param>
        /// <param name="test_count"></param>
        private void DoExecuteTests(IBasicRouterDataSource<EdgeData> data, OsmRoutingInterpreter interpreter, int test_count)
        {
            if (data.VertexCount == 0)
            {
                OsmSharp.Tools.Output.OutputStreamHost.WriteLine("0 vertices in data source!");
                return;
            }

            // build router;
            IBasicRouter<EdgeData> basic_router = this.BuildBasicRouter(data);
            IRouter<RouterPoint> router = this.BuildRouter(data, interpreter, basic_router);

            // generate random route pairs.
            List<KeyValuePair<RouterPoint, RouterPoint>> test_pairs =
                new List<KeyValuePair<RouterPoint, RouterPoint>>(test_count);
            while (test_pairs.Count < test_count)
            {
                uint first = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                    (int)data.VertexCount - 1) + 1;
                uint second = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                    (int)data.VertexCount - 1) + 1;

                float latitude_first, longitude_first;
                data.GetVertex(first, out latitude_first, out longitude_first);
                RouterPoint first_resolved = router.Resolve(VehicleEnum.Car, 
                    new GeoCoordinate(latitude_first, longitude_first));

                float latitude_second, longitude_second;
                data.GetVertex(second, out latitude_second, out longitude_second);
                RouterPoint second_resolved = router.Resolve(VehicleEnum.Car, 
                    new GeoCoordinate(latitude_second, longitude_second));

                if (((second_resolved != null) &&
                    (first_resolved != null)) &&
                    (router.CheckConnectivity(VehicleEnum.Car, first_resolved, 30) &&
                    router.CheckConnectivity(VehicleEnum.Car, second_resolved, 30)))
                {
                    test_pairs.Add(new KeyValuePair<RouterPoint, RouterPoint>(
                        first_resolved, second_resolved));
                }

                OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(test_pairs.Count, test_count, "Osm.Routing.Test.Point2Point.Point2PointTest<EdgeData>.Execute",
                    "Building pairs list...");
            }

            long before = DateTime.Now.Ticks;
            for(int idx = 0; idx < test_pairs.Count; idx++)
            {
                KeyValuePair<RouterPoint, RouterPoint> pair = test_pairs[idx];
                router.Calculate(VehicleEnum.Car, pair.Key, pair.Value);

                OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(idx, test_pairs.Count, "Osm.Routing.Test.Point2Point.Point2PointTest<EdgeData>.Execute",
                    "Routing pairs...");
            }
            long after = DateTime.Now.Ticks;
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine();
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine(string.Format("Average calculation time for {0} random routes: {1}ms",
                test_count, (new TimeSpan((after - before) / test_count)).TotalMilliseconds.ToString()));
        }

        /// <summary>
        /// Executes some general random query performance evaluation(s).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="test_count"></param>
        public void ExecuteComparisonTests(string name, int test_count)
        {
            string xml_embedded = string.Format("OsmSharp.Routing.Osm.Test.TestData.{0}.osm", name);

            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();
            this.ExecuteComparisionTests(name,
                Assembly.GetExecutingAssembly().GetManifestResourceStream(xml_embedded), false, interpreter, test_count);
        }

        /// <summary>
        /// Executes a test comparing two routes.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ExecuteComparisonTest(string name, GeoCoordinate from, GeoCoordinate to)
        {
            string xml_embedded = string.Format("OsmSharp.Routing.Osm.Test.TestData.{0}.osm", name);
            Stream data_stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(xml_embedded);

            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();
            // build the reference router.
            IRouter<RouterPoint> reference_router = this.BuildReferenceRouter(data_stream, false,
                interpreter);
            if (reference_router != null)
            {
                // build the new router.
                IBasicRouterDataSource<EdgeData> data = this.BuildData(data_stream, false,
                        interpreter, null);
                IBasicRouter<EdgeData> basic_router = this.BuildBasicRouter(data);
                IRouter<RouterPoint> router = this.BuildRouter(data, interpreter, basic_router);

                OsmSharpRoute route = router.Calculate(VehicleEnum.Car,
                    router.Resolve(VehicleEnum.Car, from), router.Resolve(VehicleEnum.Car, to));
                OsmSharpRoute route_reference = reference_router.Calculate(VehicleEnum.Car,
                    reference_router.Resolve(VehicleEnum.Car, from), reference_router.Resolve(VehicleEnum.Car, to));
            }
        }

        /// <summary>
        /// Executes a comparison test, comparing the routes from the reference implementation to the implementation being tested.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data_stream"></param>
        /// <param name="pbf"></param>
        private void ExecuteComparisionTests(string name, Stream data_stream, bool pbf,
            IRoutingInterpreter interpreter, int test_count)
        {
            OsmSharp.Tools.Output.OutputStreamHost.WriteLine("Comparison Test {0}: {1}x", name, test_count);

            // build the reference router.
            IRouter<RouterPoint> reference_router = this.BuildReferenceRouter(data_stream, pbf,
                interpreter);

            if (reference_router != null)
            {
                // build the new router.
                IBasicRouterDataSource<EdgeData> data = this.BuildData(data_stream, pbf,
                        interpreter, null);
                IBasicRouter<EdgeData> basic_router = this.BuildBasicRouter(data);
                IRouter<RouterPoint> router = this.BuildRouter(data, interpreter, basic_router);

                // generate random route pairs.
                List<KeyValuePair<RouterPoint, RouterPoint>> test_pairs =
                    new List<KeyValuePair<RouterPoint, RouterPoint>>(test_count);
                List<KeyValuePair<RouterPoint, RouterPoint>> test_pairs_reference =
                    new List<KeyValuePair<RouterPoint, RouterPoint>>(test_count);
                while (test_pairs.Count < test_count)
                {
                    uint first = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                        (int)data.VertexCount - 1) + 1;
                    uint second = (uint)OsmSharp.Tools.Math.Random.StaticRandomGenerator.Get().Generate(
                        (int)data.VertexCount - 1) + 1;

                    float latitude_first, longitude_first;
                    data.GetVertex(first, out latitude_first, out longitude_first);
                    RouterPoint first_resolved = router.Resolve(VehicleEnum.Car, 
                        new GeoCoordinate(latitude_first, longitude_first));
                    RouterPoint first_resolved_reference = reference_router.Resolve(VehicleEnum.Car, 
                        new GeoCoordinate(latitude_first, longitude_first));

                    float latitude_second, longitude_second;
                    data.GetVertex(second, out latitude_second, out longitude_second);
                    RouterPoint second_resolved = router.Resolve(VehicleEnum.Car, 
                        new GeoCoordinate(latitude_second, longitude_second));
                    RouterPoint second_resolved_reference = reference_router.Resolve(VehicleEnum.Car, 
                        new GeoCoordinate(latitude_second, longitude_second));

                    if ((((second_resolved != null) &&
                        (first_resolved != null)) &&
                        (router.CheckConnectivity(VehicleEnum.Car, first_resolved, 30) &&
                        router.CheckConnectivity(VehicleEnum.Car, second_resolved, 30))) &&
                        (((second_resolved_reference != null) &&
                            (first_resolved_reference != null)) &&
                            (reference_router.CheckConnectivity(VehicleEnum.Car, first_resolved_reference, 30) &&
                            reference_router.CheckConnectivity(VehicleEnum.Car, second_resolved_reference, 30))))
                    {
                        test_pairs.Add(new KeyValuePair<RouterPoint, RouterPoint>(
                            first_resolved, second_resolved));
                        test_pairs_reference.Add(new KeyValuePair<RouterPoint, RouterPoint>(
                            first_resolved, second_resolved));
                    }

                    OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(test_pairs.Count, test_count, "Osm.Routing.Test.Point2Point.Point2PointTest<EdgeData>.Execute",
                        "Building pairs list...");
                }

                int min_length = int.MaxValue;
                for (int idx = 0; idx < test_pairs.Count; idx++)
                {
                    KeyValuePair<RouterPoint, RouterPoint> pair = test_pairs[idx];
                    OsmSharpRoute route = router.Calculate(VehicleEnum.Car, 
                        test_pairs[idx].Key, test_pairs[idx].Value);
                    OsmSharpRoute reference_route = reference_router.Calculate(VehicleEnum.Car, 
                        test_pairs_reference[idx].Key, test_pairs_reference[idx].Value);

                    if (reference_route != null)
                    { // the reference route was found!
                        if (route == null)
                        { // the other routes does not exist!
                            OsmSharp.Tools.Output.OutputStreamHost.WriteLine();
                            OsmSharp.Tools.Output.OutputStreamHost.WriteLine(
                                "Route does not exist:{0} {1}m", reference_route.Entries.Length,
                                    reference_route.TotalDistance);

                            reference_route.SaveAsGpx(new FileInfo(
                                string.Format(@"c:\temp\route_{0}_reference_not_exist.gpx", reference_route.Entries.Length)));
                        }
                        else if (System.Math.Abs(route.TotalDistance - reference_route.TotalDistance) > 0.00001)
                        { // the routes are not equal.
                            OsmSharp.Tools.Output.OutputStreamHost.WriteLine();
                            OsmSharp.Tools.Output.OutputStreamHost.WriteLine(
                                "Routes do not match:{0} with {1}m difference", reference_route.Entries.Length,
                                    route.TotalDistance - reference_route.TotalDistance);

                            if (min_length > reference_route.Entries.Length)
                            {
                                route.SaveAsGpx(new FileInfo(
                                    string.Format(@"c:\temp\route_{0}.gpx", reference_route.Entries.Length)));
                                reference_route.SaveAsGpx(new FileInfo(
                                    string.Format(@"c:\temp\route_{0}_reference.gpx", reference_route.Entries.Length)));

                                min_length = reference_route.Entries.Length;
                            }
                        }
                    }

                    OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(idx, test_pairs.Count, "Osm.Routing.Test.Point2Point.Point2PointTest<EdgeData>.Execute",
                        "Routing pairs...");
                }
                OsmSharp.Tools.Output.OutputStreamHost.WriteLine();
            }
        }

        #region Abstract Router Building Functions

        /// <summary>
        /// Builds a router.
        /// </summary>
        /// <returns></returns>
        public abstract IRouter<RouterPoint> BuildRouter(IBasicRouterDataSource<EdgeData> data,
            IRoutingInterpreter interpreter, IBasicRouter<EdgeData> router_basic);

        /// <summary>
        /// Builds data source.
        /// </summary>
        /// <param name="interpreter"></param>
        /// <returns></returns>
        public abstract IBasicRouterDataSource<EdgeData> BuildData(Stream data, bool pbf, IRoutingInterpreter interpreter, GeoCoordinateBox box);

        /// <summary>
        /// Builds a basic router.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract IBasicRouter<EdgeData> BuildBasicRouter(IBasicRouterDataSource<EdgeData> data);

        /// <summary>
        /// Builds the reference router.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="interpreter"></param>
        /// <returns></returns>
        public abstract IRouter<RouterPoint> BuildReferenceRouter(Stream data_stream, bool pbf, IRoutingInterpreter interpreter);

        #endregion
    }
}
