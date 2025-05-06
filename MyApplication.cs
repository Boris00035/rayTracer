// TODO:
// Implement the plane debugDraw and Intersection methods

// Take user input to change the view and position of camera
// 3d view maken

// vragen: Waarom moet de nearest primitive in de intersection class? hoe werkt dat? (staat in de opdracht pdf)

using Assimp;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Microsoft.VisualBasic;

namespace Template
{
    public class Camera {
        public Vector3 position;
        public Vector3 lookAtDirection;
        public Vector3 upDirection;
        public float width;
        public float height;
        public double focalLength;

        public Camera(Vector3 position, Vector3 lookAtDirection, Vector3 upDirection, float width, float height, double focalLength) {
            this.position = position;
            this.lookAtDirection = lookAtDirection;
            this.upDirection = upDirection;
            this.width = width;
            this.height = height;
            this.focalLength = focalLength;
        }
    }

    public class LightSource {
        Vector3 location;
        double intensity;

        public LightSource(Vector3 location, double intensity) {
            this.location = location;
            this.intensity = intensity;
        }
    } 

    public class Ray {
        public Vector3 normal;
        public Vector3 startingPosition; 

        public Ray(Vector3 normal, Vector3 startingPosition) {
            this.normal = normal;
            this.startingPosition = startingPosition;
        }
    }

    public class Intersection {
        public Vector3 intersectionPoint;
        public double distanceToCamera;
        // GeometryPrimitive nearestPrimitive;
        public Vector3 surfaceNormal;
        public Ray ray;

        public Intersection(Vector3 intersectionPoint, double distanceToCamera, Vector3 surfaceNormal, Ray ray) {
            this.intersectionPoint = intersectionPoint;
            this.distanceToCamera = distanceToCamera;
            // this.nearestPrimitive = nearestPrimitive;
            this.surfaceNormal = surfaceNormal;
            this.ray = ray;
        }
    }

    public abstract class GeometryPrimitive {
        public abstract Intersection Intersect(Ray ray);
        public abstract void DebugDraw(Surface screen);
    }

    public class Plane : GeometryPrimitive {
        Vector3 normal;
        double distance;
        double size;

        public Plane(Vector3 normal, double distance, double size){
            this.normal = normal;
            this.distance = distance;
            this.size = size;
        }
        
        public override Intersection Intersect(Ray ray) {
            Plane examplePlane = new Plane(new Vector3(0,0,1), 10, 10);
            Vector3 intersectionNormal = new Vector3(0,0,-1);
            return new Intersection(new Vector3(0,0,0), 5.0, intersectionNormal, ray);
        }

        public override void DebugDraw(Surface screen) {
            
        }
    }

    public class Sphere : GeometryPrimitive {
        public Vector3 position;
        public double radius;
        public Color3 color;

        public Sphere(Vector3 position, double radius, Color3 color){
            this.position = position;
            this.radius = radius;
            this.color = color;
        }

        public override Intersection Intersect(Ray ray) {
            Vector3 differenceVector = ray.startingPosition - position;
            double quadraticTerm = Vector3.Dot(ray.normal,ray.normal);
            double linearTerm = 2 * Vector3.Dot(ray.normal,differenceVector);
            double constantTerm = Vector3.Dot(differenceVector,differenceVector) - (double)(radius*radius);

            double intersectionDistance;
            double discriminant = linearTerm*linearTerm - 4*quadraticTerm*constantTerm;
            // Console.WriteLine(discriminant);
            if (discriminant < 0) {
                intersectionDistance = float.MaxValue / 2;
            }
            else {
                intersectionDistance = Math.Min(
                Math.Abs((-1*linearTerm + Math.Sqrt(discriminant)) / (2*quadraticTerm)),
                Math.Abs((-1*linearTerm - Math.Sqrt(discriminant)) / (2*quadraticTerm))
                );
            }

            Vector3 intersectionPoint = ray.normal * (float)intersectionDistance + ray.startingPosition; 

            Vector3 surfaceNormal = position - intersectionPoint;
            surfaceNormal.Normalize(); 
            
            return new Intersection(intersectionPoint, intersectionDistance, surfaceNormal, ray);
        }

        public override void DebugDraw(Surface screen) {
            double PI = 3.1415926535897932384626433832795028;
            double interval = 2.0 * PI / 100.0;
            
            for (int i=0; i < 100; i++) {
                screen.Line((int)Math.Round(position[0] + radius * Math.Cos(i * interval)), 
                            (int)Math.Round(position[2] + radius * Math.Sin(i * interval)),
                            (int)Math.Round(position[0] + radius * Math.Cos((i+1) * interval)), 
                            (int)Math.Round(position[2] + radius * Math.Sin((i+1) * interval)),
                            color);
            }
        }
    }

    public class SceneGeometry {
        public GeometryPrimitive[] primitives;
        LightSource[] lightSources;

        public SceneGeometry(GeometryPrimitive[] primitives, LightSource[] lightSources) {
            this.primitives = primitives;
            this.lightSources = lightSources;
        }
    }

    public class RayTracer {
        SceneGeometry scene;
        Camera camera;
        Surface screen;

        public RayTracer(SceneGeometry scene, Camera camera, Surface screen) {
            this.scene = scene;
            this.camera = camera;
            this.screen = screen;
        }

        public void Render(bool debugMode) {
            if (debugMode == true) {
                Vector3 focalPoint = camera.position - Vector3.Multiply(camera.lookAtDirection, (float)camera.focalLength);
                
                // Console.WriteLine((camera.position[0],camera.position[2]));
                // Console.WriteLine((focalPoint[0],focalPoint[2]));

                Vector3 widthVector = Vector3.Cross(camera.upDirection, camera.lookAtDirection);
                widthVector.Normalize();

                screen.Bar((int)Math.Round(focalPoint[0]) - 1,
                        (int)Math.Round(focalPoint[2]) - 1,
                        (int)Math.Round(focalPoint[0]) + 1,
                        (int)Math.Round(focalPoint[2]) + 1, 
                        new Color3(255, 255,0));

                List<Intersection> intersectionArray = new List<Intersection>();

                for (int collumnPixel = - (int)Math.Round(0.5*screen.width); collumnPixel <= (int)Math.Round(0.5*screen.width); collumnPixel = collumnPixel + 10)
                {   
                    // if (collumnPixel != - (int)Math.Round(0.5*screen.width)) {
                    //     break;
                    // }
                    float cameraSizeStep = camera.width / screen.width;
                    Vector3 rayNormal = camera.position + cameraSizeStep * collumnPixel * widthVector - focalPoint;
                    rayNormal.Normalize();

                    Ray ray = new Ray(rayNormal, focalPoint); 

                    foreach (GeometryPrimitive primitive in scene.primitives) {
                        primitive.DebugDraw(screen);
                        intersectionArray.Add(primitive.Intersect(ray));
                    }

                    Intersection defaultIntersection = new Intersection(new Vector3(0,0,0), double.MaxValue / 2, new Vector3(0,0,0), ray);
                    Intersection closestIntersection = intersectionArray
                        .DefaultIfEmpty(defaultIntersection)
                        .OrderBy(i => i.distanceToCamera)
                        .First();
                    
                    screen.Line((int)Math.Round(focalPoint[0]), 
                                (int)Math.Round(focalPoint[2]), 
                                (int)Math.Round(closestIntersection.intersectionPoint[0]), 
                                (int)Math.Round(closestIntersection.intersectionPoint[2]), 
                                new Color3(255,255,0));
                    intersectionArray = new List<Intersection>();
                }

                Vector3 leftScreenPoint = camera.position + (float)0.5 * camera.width * widthVector;
                Vector3 rightScreenPoint = camera.position - (float)0.5 * camera.width * widthVector; 
                
                screen.Line((int)Math.Round(leftScreenPoint[0]),
                            (int)Math.Round(leftScreenPoint[2]),
                            (int)Math.Round(rightScreenPoint[0]),
                            (int)Math.Round(rightScreenPoint[2]),
                            new Color3(255,255,255)
                );
                
            } else {
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
            object? mesh = Util.ImportMesh("assets/cube.obj");
        }
        // tick: renders one frame
        private TimeSpan deltaTime = new();
        private uint frames = 0;
        private string timeString = "---- ms/frame";

        public bool debugMode = true;
        public void Tick()
        {
            timer.Restart();
            screen.Clear(0);

            SceneGeometry scene = new SceneGeometry(
                [new Sphere(new Vector3(400, 0, 50), 100, (255,0,0)), new Sphere(new Vector3(200, 0, 50), 100, (0,255,0))], 
                [new LightSource(new Vector3(0, 5, 0), 0.5)]);
            Camera camera = new Camera(
                new Vector3(300, 0, 300), 
                new Vector3(0, 0, -1), 
                new Vector3(0, 1, 0), 
                50,
                10,
                30.0);
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