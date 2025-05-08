// TODO:
// Implement the plane debugDraw and Intersection methods

// Take user input to change the view and position of camera
// implement shading

// vragen: Waarom moet de nearest primitive in de intersection class? hoe werkt dat? (staat in de opdracht pdf)

using Assimp;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Microsoft.VisualBasic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;

namespace Template
{
    public class Camera
    {
        public Vector3 position;
        public Vector3 lookAtDirection;
        public Vector3 upDirection;
        public float width;
        public float height;
        public double focalLength;

        public Camera(Vector3 position, Vector3 lookAtDirection, Vector3 upDirection, float width, float height, double focalLength)
        {
            this.position = position;
            this.lookAtDirection = lookAtDirection;
            this.upDirection = upDirection;
            this.width = width;
            this.height = height;
            this.focalLength = focalLength;
        }
    }

    public class LightSource
    {
        public Vector3 position;
        public double intensity;

        public LightSource(Vector3 position, double intensity)
        {
            this.position = position;
            this.intensity = intensity;
        }

        public void DebugDraw(Surface screen)
        {
            screen.Bar((int)Math.Round(position[0]) - 1,
                        (int)Math.Round(position[2]) - 1,
                        (int)Math.Round(position[0]) + 1,
                        (int)Math.Round(position[2]) + 1,
                        new Color3(0, 1, 1));
        }

    }

    public class Ray
    {
        public Vector3 normal;
        public Vector3 startingPosition;

        public Ray(Vector3 normal, Vector3 startingPosition)
        {
            this.normal = normal;
            this.startingPosition = startingPosition;
        }
    }

    // make abstract Ray class, then two classes lightRay and cameraRay.
    // Then have every intersection a Ray field, and every (geometry) primitive a list of closestLightIntersections, and possibly also store the closestCameraIntersection 

    public class Intersection
    {
        public Vector3 intersectionPoint;
        public GeometryPrimitive intersectedPrimitive;
        public double distanceToStartingPoint;
        // GeometryPrimitive nearestPrimitive;
        public Vector3 surfaceNormal;
        public Ray ray;

        public Intersection(Vector3 intersectionPoint, GeometryPrimitive intersectedPrimitive, double distanceToStartingPoint, Vector3 surfaceNormal, Ray ray)
        {
            this.intersectionPoint = intersectionPoint;
            this.intersectedPrimitive = intersectedPrimitive;
            this.distanceToStartingPoint = distanceToStartingPoint;
            // this.nearestPrimitive = nearestPrimitive;
            this.surfaceNormal = surfaceNormal;
            this.ray = ray;
        }
    }

    public abstract class GeometryPrimitive
    {
        public abstract Intersection? Intersect(Ray ray);
        public abstract void DebugDraw(Surface screen);
        public Vector3 position;
        public Color3 color;
        // this field is currently not used, the closestlightintersection is just calculated when needed. But with some refactoring this can probably be extracted fairly cheaply from the "main" loop.
        public List<Intersection> closestLightIntersections;

        protected GeometryPrimitive(Vector3 position, Color3 color, List<Intersection> closestLightIntersections)
        {
            this.color = color;
            this.position = position;
            this.closestLightIntersections = closestLightIntersections;
        }
    }

    public class Plane : GeometryPrimitive
    {
        Vector3 normal;
        float width;
        float height;

        public Plane(Vector3 position, Vector3 normal, float width, float height, Color3 color, List<Intersection> closestIntersections) : base(position, color, closestIntersections)
        {
            this.normal = normal;
            this.width = width;
            this.height = height;
        }

        public override Intersection? Intersect(Ray ray)
        {
            Plane examplePlane = new Plane((1, 0, 0), new Vector3(0, 0, 1), 10, 10,  (0,0,0), []);
            Vector3 intersectionNormal = new Vector3(0, 0, -1);
            return new Intersection(new Vector3(0, 0, 0), examplePlane, 5.0, intersectionNormal, ray);
        }

        public override void DebugDraw(Surface screen)
        {

        }
    }

    public class Sphere : GeometryPrimitive
    {
        public double radius;

        public Sphere(Vector3 position, double radius, Color3 color, List<Intersection> closestIntersections) : base(position, color, closestIntersections)
        {
            this.radius = radius;
        }


        public override Intersection? Intersect(Ray ray)
        {
            Vector3 differenceVector = ray.startingPosition - position;
            double quadraticTerm = Vector3.Dot(ray.normal, ray.normal);
            double linearTerm = 2 * Vector3.Dot(ray.normal, differenceVector);
            double constantTerm = Vector3.Dot(differenceVector, differenceVector) - (double)(radius * radius);

            double intersectionDistance;
            double discriminant = linearTerm * linearTerm - 4 * quadraticTerm * constantTerm;
            // Console.WriteLine(discriminant);
            if (discriminant < 0)
            {
                return null;
            }
            else
            {
                intersectionDistance = Math.Min(
                Math.Abs((-1 * linearTerm + Math.Sqrt(discriminant)) / (2 * quadraticTerm)),
                Math.Abs((-1 * linearTerm - Math.Sqrt(discriminant)) / (2 * quadraticTerm))
                );
            }

            Vector3 intersectionPoint = ray.normal * (float)intersectionDistance + ray.startingPosition;

            Vector3 surfaceNormal = position - intersectionPoint;
            surfaceNormal.Normalize();

            return new Intersection(intersectionPoint, this, intersectionDistance, surfaceNormal, ray);
        }

        public override void DebugDraw(Surface screen)
        {
            double PI = 3.1415926535897932384626433832795028;
            double interval = 2.0 * PI / 100.0;

            double adjustedRadius = Math.Sqrt(radius * radius - position[1] * position[1]);
            for (int i = 0; i < 100; i++)
            {
                screen.Line((int)Math.Round(position[0] + adjustedRadius * Math.Cos(i * interval)),
                            (int)Math.Round(position[2] + adjustedRadius * Math.Sin(i * interval)),
                            (int)Math.Round(position[0] + adjustedRadius * Math.Cos((i + 1) * interval)),
                            (int)Math.Round(position[2] + adjustedRadius * Math.Sin((i + 1) * interval)),
                            color);
            }
        }
    }

    public class SceneGeometry
    {
        public GeometryPrimitive[] primitives;
        public LightSource[] lightSources;

        public SceneGeometry(GeometryPrimitive[] primitives, LightSource[] lightSources)
        {
            this.primitives = primitives;
            this.lightSources = lightSources;
        }
    }

    public class RayTracer
    {
        SceneGeometry scene;
        Camera camera;
        Surface screen;

        public RayTracer(SceneGeometry scene, Camera camera, Surface screen)
        {
            this.scene = scene;
            this.camera = camera;
            this.screen = screen;
        }

        public void Render(bool debugMode)
        {
            if (debugMode == true)
            {
                Vector3 focalPoint = camera.position - camera.lookAtDirection * (float)camera.focalLength;

                // Console.WriteLine((camera.position[0],camera.position[2]));
                // Console.WriteLine((focalPoint[0],focalPoint[2]));

                Vector3 leftDirection = Vector3.Cross(camera.upDirection, camera.lookAtDirection);
                leftDirection.Normalize();

                screen.Bar((int)Math.Round(focalPoint[0]) - 1,
                        (int)Math.Round(focalPoint[2]) - 1,
                        (int)Math.Round(focalPoint[0]) + 1,
                        (int)Math.Round(focalPoint[2]) + 1,
                        new Color3(1, 1, 0));

                List<Intersection> intersectionArray = new List<Intersection>();
                float cameraSizeStep = camera.width / screen.width;


                for (int collumnPixel = -(int)Math.Round(0.5 * screen.width); collumnPixel <= (int)Math.Round(0.5 * screen.width); collumnPixel = collumnPixel + 5)
                {
                    // if (collumnPixel != - (int)Math.Round(0.5*screen.width)) {
                    //     break;
                    // }
                    Vector3 rayNormal = camera.position + cameraSizeStep * collumnPixel * leftDirection - focalPoint;
                    rayNormal.Normalize();

                    Ray ray = new Ray(rayNormal, focalPoint);

                    foreach (LightSource lightSource in scene.lightSources)
                    {
                        lightSource.DebugDraw(screen);
                    }

                    foreach (GeometryPrimitive primitive in scene.primitives)
                    {
                        primitive.DebugDraw(screen);
                        Intersection? intersectResult = primitive.Intersect(ray);

                        if (intersectResult != null)
                        {
                            intersectionArray.Add(intersectResult);
                        }
                    }

                    Vector3 zeroVector = new Vector3(0, 0, 0);

                    if (intersectionArray.Count == 0)
                    {
                        screen.Line((int)Math.Round(focalPoint[0]),
                                    (int)Math.Round(focalPoint[2]),
                                    (int)Math.Round(1000000 * ray.normal[0]),
                                    (int)Math.Round(1000000 * ray.normal[2]),
                                    new Color3(1, 1, 0));
                    }
                    else
                    {
                        Intersection closestIntersection = intersectionArray
                        .OrderBy(i => i.distanceToStartingPoint)
                        .First();
                        screen.Line((int)Math.Round(focalPoint[0]),
                                    (int)Math.Round(focalPoint[2]),
                                    (int)Math.Round(closestIntersection.intersectionPoint[0]),
                                    (int)Math.Round(closestIntersection.intersectionPoint[2]),
                                    new Color3(1, 1, 0));

                        bool visibility = false;

                        foreach (LightSource lightSource in scene.lightSources)
                        {
                            List<Intersection> lightRayIntersectionArray = new List<Intersection>();
                            Vector3 lightRayNormal = closestIntersection.intersectionPoint - lightSource.position;

                            Ray lightRay = new Ray(lightRayNormal, lightSource.position);
                            foreach (GeometryPrimitive primitive in scene.primitives)
                            {
                                Intersection? intersectionResult = primitive.Intersect(lightRay);
                                if (intersectionResult != null)
                                {
                                    // Should never be null as it should always intersection with the closestIntersection.intersectionPoint
                                    lightRayIntersectionArray.Add(intersectionResult);
                                }
                            }

                            if (lightRayIntersectionArray.Count() != 0)
                            {
                                Intersection closestLightRayIntersection = lightRayIntersectionArray
                                    .OrderBy(i => i.distanceToStartingPoint)
                                    .First();                                

                                screen.Line((int)Math.Round(lightSource.position[0]),
                                            (int)Math.Round(lightSource.position[2]),
                                            (int)Math.Round(closestLightRayIntersection.intersectionPoint[0]),
                                            (int)Math.Round(closestLightRayIntersection.intersectionPoint[2]),
                                            new Color3(1, 1, 1));
                                // Console.WriteLine(closestLightRayIntersection.intersectionPoint);
                                // Console.WriteLine(closestIntersection.intersectionPoint);
                                // Console.WriteLine("\n");
                                if ((closestLightRayIntersection.intersectionPoint - closestIntersection.intersectionPoint).Length < 0.1)
                                {
                                    visibility = true;
                                }
                            }
                            // lightRayIntersectionArray = new List<Intersection>();


                        }
                    }

                    intersectionArray = new List<Intersection>();
                }

                Vector3 leftScreenPoint = camera.position + (float)0.5 * camera.width * leftDirection;
                Vector3 rightScreenPoint = camera.position - (float)0.5 * camera.width * leftDirection;

                screen.Line((int)Math.Round(leftScreenPoint[0]),
                            (int)Math.Round(leftScreenPoint[2]),
                            (int)Math.Round(rightScreenPoint[0]),
                            (int)Math.Round(rightScreenPoint[2]),
                            new Color3(1, 1, 1)
                );
                


            }
            else
            {
                Vector3 focalPoint = camera.position - camera.lookAtDirection * (float)camera.focalLength;
                Vector3 leftDirection = Vector3.Cross(camera.upDirection, camera.lookAtDirection);
                // leftDirection.Normalize();

                float cameraWidthScale = camera.width / screen.width;
                float cameraHeightScale = camera.height / screen.height;

                List<Intersection> intersectionArray = new List<Intersection>();

                for (int heightPixel = 0; heightPixel <= screen.height; heightPixel++)
                {
                    for (int widthPixel = 0; widthPixel <= screen.width; widthPixel++)
                    {

                        Vector3 rayNormal = camera.position + cameraWidthScale * ((int)Math.Round(0.5 * screen.width) - widthPixel) * leftDirection + cameraHeightScale * ((int)Math.Round(0.5 * screen.height) - heightPixel) * camera.upDirection - focalPoint;
                        rayNormal.Normalize();

                        Ray ray = new Ray(rayNormal, focalPoint);

                        foreach (GeometryPrimitive primitive in scene.primitives)
                        {
                            Intersection? intersectionResult = primitive.Intersect(ray);
                            if (intersectionResult != null)
                            {
                                intersectionArray.Add(intersectionResult);
                            }


                        }

                        if (intersectionArray.Count != 0)
                        {
                            Intersection closestIntersection = intersectionArray
                                .OrderBy(i => i.distanceToStartingPoint)
                                .First();
                            


                            bool visibility = false;



                            foreach (LightSource lightSource in scene.lightSources)
                            {
                                List<Intersection> lightRayIntersectionArray = new List<Intersection>();
                                Vector3 lightRayNormal = closestIntersection.intersectionPoint - lightSource.position;

                                Ray lightRay = new Ray(lightRayNormal, lightSource.position);
                                foreach (GeometryPrimitive primitive in scene.primitives)
                                {
                                    Intersection? intersectionResult = primitive.Intersect(lightRay);
                                    if (intersectionResult != null)
                                    {
                                        // Should never be null as it should always intersection with the closestIntersection.intersectionPoint
                                        lightRayIntersectionArray.Add(intersectionResult);
                                    }
                                }

                                if (lightRayIntersectionArray.Count() != 0)
                                {
                                    Intersection closestLightRayIntersection = lightRayIntersectionArray
                                        .OrderBy(i => i.distanceToStartingPoint)
                                        .First();

                                    // closestLightRayIntersection


                                    // Console.WriteLine(closestLightRayIntersection.intersectionPoint);
                                    // Console.WriteLine(closestIntersection.intersectionPoint);
                                    // Console.WriteLine("\n");
                                    if ((closestLightRayIntersection.intersectionPoint - closestIntersection.intersectionPoint).Length < 0.1)
                                    {
                                        visibility = true;
                                    }
                                }
                                lightRayIntersectionArray = new List<Intersection>();


                            }
                            Color3 color = new Color3(0, 0, 0);
                            float colorScaleFactor = 0;
                            if (visibility == true)
                            {
                                color = closestIntersection.intersectedPrimitive.color;
                                // This is not optimal, as it takes the minimal distance between the primitive and the source, not the minimal distance of rays that actually hit the primitive.
                                float minimalDistance = (closestIntersection.ray.startingPosition - closestIntersection.intersectedPrimitive.position).Length;
                                colorScaleFactor = minimalDistance / (float)closestIntersection.distanceToStartingPoint - 1;
                            }

                            Color3 scaledColor = new Color3(
                                closestIntersection.intersectedPrimitive.color.R * colorScaleFactor,
                                closestIntersection.intersectedPrimitive.color.G * colorScaleFactor,
                                closestIntersection.intersectedPrimitive.color.B * colorScaleFactor );
                            screen.Plot(widthPixel, heightPixel, color);
                        }
                        // Console.WriteLine(closestIntersection.intersectionPoint);

                        intersectionArray = new List<Intersection>();
                    }
                }

            }

        }
    }

    class MyApplication
    {

        // member variables
        public Surface screen;
        private readonly Stopwatch timer = new();
        // constructor
        public MyApplication(Surface screen)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            this.screen = screen;
        }
        // initialize
        public void Init()
        {
            // (optional) example of how you can load a triangle mesh in any file format supported by Assimp
            //object? mesh = Util.ImportMesh("../../../assets/cube.obj");
        }
        // tick: renders one frame
        private TimeSpan deltaTime = new();
        private uint frames = 0;
        private string timeString = "---- ms/frame";

        public bool debugMode = false;
        public void Tick()
        {
            timer.Restart();
            screen.Clear(new Color3((float)0.2, (float)0.2, (float)0.2));

            SceneGeometry scene = new SceneGeometry(
                [
                new Sphere(new Vector3(400, 50, 50), 100, (1,0,0), []),
                new Sphere(new Vector3(100, 0, 0), 100, (0,1,0), []),
                ],
                [
                new LightSource(new Vector3(1000, 0, 300), 0.5),
                new LightSource(new Vector3(100, 0, 500), 0.5)]);
            Camera camera = new Camera(
                new Vector3(300, 0, 300),
                new Vector3(0, 0, -1),
                new Vector3(0, 1, 0),
                50,
                10,
                10.0);
            RayTracer rayTracer = new RayTracer(scene, camera, screen);



            rayTracer.Render(debugMode);

            deltaTime += timer.Elapsed;
            frames++;
            if (deltaTime.TotalSeconds > 0)
            {
                // Console.WriteLine(deltaTime);

                timeString = (deltaTime.TotalMilliseconds / frames).ToString("F1") + " ms/frame";
                frames = 0;
                deltaTime = TimeSpan.Zero;
            }


            screen.PrintOutlined(timeString, 2, 2, Color4.White);
        }
    }
}