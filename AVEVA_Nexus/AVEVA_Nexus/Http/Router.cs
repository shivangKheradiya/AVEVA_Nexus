using AVEVA_Nexus.Controllers;
using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVEVA_Nexus.Http
{
    public class Router
    {
        private readonly PmlController _pml;
        private readonly ElementController _element;

        public Router()
        {
            _pml = new PmlController();
            _element = new ElementController();
        }

        //public object Route(RouteContext context)
        //{
        //    if (path == "/api/pml/variable/string/test")
        //    {
        //        return _pml.GetString("Test");
        //    }
        //
        //    if (path == "/api/pml/variable/real/test")
        //    {
        //        return _pml.GetReal("Test");
        //    }
        //
        //    if (path == "/api/pml/variable/boolean/test")
        //    {
        //        return _pml.GetBoolean("Test");
        //    }
        //
        //    if (path == "/api/pml/variable/array/test")
        //    {
        //        return _pml.GetArray("Test");
        //    }
        //
        //    if (path == "/api/element/attribute/test")
        //    {
        //        return _element.GetAttribute(
        //            "=1234/5678",
        //            "NAME");
        //    }
        //
        //    throw new NexusException(
        //        "Route Not Found",
        //        404);
        //}


        public object Route(RouteContext context)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("ROUTER INVOKED");
            Console.WriteLine("========================================");

            Console.WriteLine($"Method      : {context.Method}");
            Console.WriteLine($"Path        : {context.Path}");
            Console.WriteLine($"Body        : {context.Body}");

            if (context.Query != null)
            {
                Console.WriteLine("Query Parameters:");

                foreach (string key in context.Query.Keys)
                {
                    Console.WriteLine($"  {key} = {context.Query[key]}");
                }
            }

            string[] segments =
                context.Path
                    .Trim('/')
                    .Split('/');

            Console.WriteLine($"Segment Count : {segments.Length}");

            for (int i = 0; i < segments.Length; i++)
            {
                Console.WriteLine($"Segment[{i}] = '{segments[i]}'");
            }

            if (segments.Length < 2)
            {
                throw new NexusException(
                    $"Invalid route. Segment count = {segments.Length}",
                    400);
            }

            Console.WriteLine($"Root Segment = {segments[0]}");
            Console.WriteLine($"Module       = {segments[1]}");

            if (segments[1].Equals(
                "PML",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Routing to PML");
                return RoutePml(context, segments);
            }

            if (segments[1].Equals(
                "ELEMENT",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Routing to Element");
                return RouteElement(context, segments);
            }

            throw new NexusException(
                $"Route Not Found. Module '{segments[1]}' not supported.",
                404);
        }


        private object RoutePml(
            RouteContext context,
            string[] segments)
        {
            if (segments.Length >= 3 &&
                segments[2].Equals(
                    "Execute",
                    StringComparison.OrdinalIgnoreCase))
            {
                return _pml.Execute(
                    context.Body);
            }

            if (segments.Length >= 5 &&
                segments[2].Equals("Variable",
                StringComparison.OrdinalIgnoreCase))
            {
                string variableType =
                    segments[3];

                string variableName =
                    segments[4];

                switch (variableType.ToLower())
                {
                    case "string":
                        return _pml.GetString(
                            variableName);

                    case "real":
                        return _pml.GetReal(
                            variableName);

                    case "boolean":
                        return _pml.GetBoolean(
                            variableName);

                    case "array":
                        return _pml.GetArray(
                            variableName);
                }
            }

            throw new NexusException(
                "Unsupported PML Route",
                404);
        }


        private object RouteElement(
            RouteContext context,
            string[] segments)
        {
            Console.WriteLine("Inside RouteElement");

            if (segments.Length >= 3 &&
                segments[2].Equals(
                    "Attribute",
                    StringComparison.OrdinalIgnoreCase))
            {
                string dbref =
                    context.Query["dbref"];

                string attribute =
                    context.Query["attribute"];

                Console.WriteLine($"DBREF     : {dbref}");
                Console.WriteLine($"ATTRIBUTE : {attribute}");

                if (string.IsNullOrWhiteSpace(dbref))
                {
                    throw new NexusException(
                        "dbref parameter missing",
                        400);
                }

                if (string.IsNullOrWhiteSpace(attribute))
                {
                    throw new NexusException(
                        "attribute parameter missing",
                        400);
                }

                return _element.GetAttribute(
                    dbref,
                    attribute);
            }

            throw new NexusException(
                "Unsupported Element Route",
                404);
        }
    }
}