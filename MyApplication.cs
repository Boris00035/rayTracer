// TODO:
// Implement the plane debugDraw and Intersection methods
// Change all Color3's to Vector3's and convert it when passing to screen.plot

// Take user input to change the view and position of camera
// implement shading

// vragen: 
// 1. Waarom moet de nearest primitive in de intersection class? hoe werkt dat? (staat in de opdracht pdf)
// 2. in de slides wordt de radiance gedeeld door r^2, de afstand van de lichtbron tot het object, moet je niet ook delen door r'^2, de afstand van het object tot het "oog"? 


using Assimp;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Threading.Tasks;


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
        public Color3 color;

        public LightSource(Vector3 position, Color3 color)
        {
            this.position = position;
            this.color = color;
        }

        public void DebugDraw(Surface screen)
        {
            if (position[0] >=0 && position[2] >=0) {
            screen.Line((int)Math.Round(position[0]) - 1,
                        (int)Math.Round(position[2]) - 1,
                        (int)Math.Round(position[0]) + 1,
                        (int)Math.Round(position[2]) + 1,
                        new Color3(0, 1, 1));
            }
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

    // possible idea: make abstract Ray class, then two classes lightRay and cameraRay.
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
        public Color3 diffuseColor;
        public Color3 specularColor;
        // this field is currently not used, the closestlightintersection is just calculated when needed. But with some refactoring this can probably be extracted fairly cheaply from the "main" loop.
        public List<Intersection> closestLightIntersections;

        protected GeometryPrimitive(Vector3 position, Color3 diffuseColor, Color3 specularColor, List<Intersection> closestLightIntersections)
        {
            this.diffuseColor = diffuseColor;
            this.position = position;
            this.diffuseColor = diffuseColor;
            this.specularColor = specularColor;
            this.closestLightIntersections = closestLightIntersections;
        }
    }

    public class Plane : GeometryPrimitive
    {
        Vector3 normal;
        float width;
        float height;

        public Plane(Vector3 position, Color3 diffuseColor, Color3 specularColor, Vector3 normal, float width, float height, List<Intersection> closestIntersections) : base(position, diffuseColor, specularColor, closestIntersections)
        {
            this.normal = normal;
            this.width = width;
            this.height = height;
        }

        public override Intersection? Intersect(Ray ray)
        {
            Vector3 differenceVector = ray.startingPosition - position;
            //double Dterm = Vector3.Dot(ray.normal, position);
            //double zeroterm = Vector3.Dot(differenceVector, ray.normal)/differenceVector;
            //double intersectionDistance = Math.Sqrt(Vector3.Dot(differenceVector, differenceVector));

            double intersectionDistance = Vector3.Dot((this.position - ray.startingPosition), this.normal) / Vector3.Dot(differenceVector, this.normal);

            Vector3 intersectionPoint = ray.normal * (float)intersectionDistance + ray.startingPosition;

            Vector3 surfaceNormal = position - intersectionPoint;
            surfaceNormal.Normalize();


            return new Intersection(intersectionPoint, this, intersectionDistance, surfaceNormal, ray);

        }

        public override void DebugDraw(Surface screen)
        {

        }
    }

    public class Sphere : GeometryPrimitive
    {
        public double radius;

        public Sphere(Vector3 position, double radius, Color3 diffuseColor, Color3 specularColor, List<Intersection> closestIntersections) : base(position, diffuseColor, specularColor, closestIntersections)
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
                            diffuseColor);
            }
        }
    }

    public class SceneGeometry
    {
        public GeometryPrimitive[] primitives;
        public LightSource[] lightSources;
        public Color3 ambientRadiance;

        public SceneGeometry(GeometryPrimitive[] primitives, LightSource[] lightSources, Color3 ambientRadiance)
        {
            this.primitives = primitives;
            this.lightSources = lightSources;
            this.ambientRadiance = ambientRadiance;
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

                screen.Line((int)Math.Round(focalPoint[0]) - 1,
                        (int)Math.Round(focalPoint[2]) - 1,
                        (int)Math.Round(focalPoint[0]) + 1,
                        (int)Math.Round(focalPoint[2]) + 1,
                        new Color3(1, 1, 0));

                List<Intersection> primaryIntersectionArray = new List<Intersection>();
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
                            primaryIntersectionArray.Add(intersectResult);
                        }
                    }

                    Vector3 zeroVector = new Vector3(0, 0, 0);

                    if (primaryIntersectionArray.Count == 0)
                    {
                        screen.Line((int)Math.Round(focalPoint[0]),
                                    (int)Math.Round(focalPoint[2]),
                                    (int)Math.Round(1000000 * ray.normal[0]),
                                    (int)Math.Round(1000000 * ray.normal[2]),
                                    new Color3(1, 1, 0));
                    }
                    else
                    {
                        Intersection closestIntersection = primaryIntersectionArray
                        .OrderBy(i => i.distanceToStartingPoint)
                        .First();
                        screen.Line((int)Math.Round(focalPoint[0]),
                                    (int)Math.Round(focalPoint[2]),
                                    (int)Math.Round(closestIntersection.intersectionPoint[0]),
                                    (int)Math.Round(closestIntersection.intersectionPoint[2]),
                                    new Color3(1, 1, 0));

                        // bool visibility = false;

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
                                // if ((closestLightRayIntersection.intersectionPoint - closestIntersection.intersectionPoint).Length < 0.1)
                                // {
                                //     visibility = true;
                                // }
                            }
                            // lightRayIntersectionArray = new List<Intersection>();


                        }
                    }

                    primaryIntersectionArray = new List<Intersection>();
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

                List<Intersection> primaryIntersectionArray = new List<Intersection>();

                for (int heightPixel = 0; heightPixel <= screen.height; heightPixel++)
                {
                    Parallel.For(0, screen.width + 1,
                    widthPixel =>
                    {
                        Vector3 rayNormal = camera.position + cameraWidthScale * ((int)Math.Round(0.5 * screen.width) - widthPixel) * leftDirection + cameraHeightScale * ((int)Math.Round(0.5 * screen.height) - heightPixel) * camera.upDirection - focalPoint;
                        rayNormal.Normalize();

                        Ray ray = new Ray(rayNormal, focalPoint);

                        List<Intersection> primaryIntersectionArray = new List<Intersection>();

                        foreach (GeometryPrimitive primitive in scene.primitives)
                        {
                            Intersection? intersectionResult = primitive.Intersect(ray);
                            if (intersectionResult != null)
                            {
                                primaryIntersectionArray.Add(intersectionResult);
                            }
                        }

                        if (primaryIntersectionArray.Count != 0)
                        {
                            Intersection closestPrimaryRayIntersection = primaryIntersectionArray
                                .OrderBy(i => i.distanceToStartingPoint)
                                .First();

                            // Base color to fake ambient lighting
                            Color3 pixelColor = new Color3(
                                closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.R * scene.ambientRadiance.R,
                                closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.G * scene.ambientRadiance.G,
                                closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.B * scene.ambientRadiance.B);

                            Color3 pixelColorToneMapped = pixelColor;

                            foreach (LightSource lightSource in scene.lightSources)
                            {
                                List<Intersection> lightRayIntersectionArray = new List<Intersection>();
                                Vector3 lightRayNormal = (closestPrimaryRayIntersection.intersectionPoint - lightSource.position).Normalized();

                                Ray lightRay = new Ray(lightRayNormal, lightSource.position);
                                foreach (GeometryPrimitive primitiveSecond in scene.primitives)
                                {
                                    Intersection? intersectionResult = primitiveSecond.Intersect(lightRay);
                                    if (intersectionResult != null)
                                    {
                                        lightRayIntersectionArray.Add(intersectionResult);
                                    }
                                }

                                if (lightRayIntersectionArray.Count != 0)
                                {
                                    Intersection closestLightRayIntersection = lightRayIntersectionArray
                                        .OrderBy(i => i.distanceToStartingPoint)
                                        .First();

                                    if (closestLightRayIntersection.intersectedPrimitive != closestPrimaryRayIntersection.intersectedPrimitive)
                                    {
                                        // This happens when the light ray is blocked by some other object, but the camera can see that point on the first object
                                        continue;
                                    }

                                    // shading logic                                     
                                    float diffuseReflectionRatio = Math.Max(0, Vector3.Dot(lightRay.normal, closestPrimaryRayIntersection.surfaceNormal)) / (float)Math.Pow(closestPrimaryRayIntersection.distanceToStartingPoint, 2);

                                    Vector3 diffuseContribution = new Vector3(
                                        diffuseReflectionRatio * lightSource.color.R * closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.R,
                                        diffuseReflectionRatio * lightSource.color.G * closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.G,
                                        diffuseReflectionRatio * lightSource.color.B * closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor.B);

                                    Vector3 lightVector = lightRay.normal * (float)closestLightRayIntersection.distanceToStartingPoint;
                                    Vector3 reflectedVectorNormal = (lightVector - 2 * Vector3.Dot(lightVector, closestLightRayIntersection.surfaceNormal) * closestLightRayIntersection.surfaceNormal).Normalized();

                                    Vector3 viewVectorNormal = (closestPrimaryRayIntersection.ray.normal * (float)closestPrimaryRayIntersection.distanceToStartingPoint).Normalized();

                                    int specularity = 50;
                                    float specularReflectionRatio = (float)Math.Pow(Math.Max(0, -1 * Vector3.Dot(reflectedVectorNormal, viewVectorNormal)), specularity) / (float)Math.Pow(closestLightRayIntersection.distanceToStartingPoint, 2);
                                    // Console.WriteLine(specularReflectionRatio);
                                    // Console.WriteLine(diffuseReflectionRatio);
                                    // Console.WriteLine("\n");

                                    Vector3 specularContribution = new Vector3(
                                        specularReflectionRatio * lightSource.color.R * closestLightRayIntersection.intersectedPrimitive.specularColor.R,
                                        specularReflectionRatio * lightSource.color.G * closestLightRayIntersection.intersectedPrimitive.specularColor.G,
                                        specularReflectionRatio * lightSource.color.B * closestLightRayIntersection.intersectedPrimitive.specularColor.B
                                    );


                                    pixelColor = new Color3(
                                        pixelColor.R + diffuseContribution[0] + specularContribution[0],
                                        pixelColor.G + diffuseContribution[1] + specularContribution[1],
                                        pixelColor.B + diffuseContribution[2] + specularContribution[2]
                                    );

                                    pixelColorToneMapped = new Color3(
                                        pixelColor.R / (1 + pixelColor.R),
                                        pixelColor.G / (1 + pixelColor.G),
                                        pixelColor.B / (1 + pixelColor.B)
                                    );


                                }
                                lightRayIntersectionArray = new List<Intersection>();


                            }

                            screen.Plot(widthPixel, heightPixel, pixelColor);
                        }
                    });
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


        Camera camera = new Camera(
            new Vector3(400, 0, 650),
            new Vector3(0, 0, -1),
            new Vector3(0, 1, 0),
            50,
            10,
            10.0);

        int tickCounter = 0;

        public void Tick()
        {
            tickCounter++;
            timer.Restart();
            screen.Clear(new Color3((float)0.2, (float)0.2, (float)0.2));



            Vector3 basePosition = new Vector3(100, 0, 300);
            Vector3 movingPosition = new Vector3(basePosition[0] + 200*(float)Math.Cos(tickCounter * 0.2), 0, basePosition[2] + 200*(float)Math.Sin(tickCounter * 0.2));
            Vector3 BasePositionPlane = new Vector3(50, -200, 100);
            Vector3 NormalVector = new Vector3(0, 1, 0);

            SceneGeometry scene = new SceneGeometry(
            [
                new Sphere(movingPosition, 40, new Color3(0,1,0), new Color3(1,1,1) * 2, []),
                new Sphere(basePosition, 100,new Color3(1,0,0), new Color3(1,1,1) * 2, []),
                new Plane(BasePositionPlane,new Color3(0,0,1), new Color3(0,1,0)*2, NormalVector, 20,20, [])
            ],
            [
                new LightSource(new Vector3(600, 0, 500), new Color3(1,1,1) * 40000 ),
                new LightSource(new Vector3(300, 0, 800), new Color3(1,1,1) * 40000 )
            ], new Color3((float)0, (float)0, (float)0));

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