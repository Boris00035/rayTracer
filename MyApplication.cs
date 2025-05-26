// TODO:
// template.cs handmatig updaten

using Microsoft.VisualBasic;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Globalization;
using IronSoftware.Drawing;
using OpenTK.Windowing.Common;
using SixLabors.ImageSharp.PixelFormats;

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
        public bool orientationLock;
        public float yaw;
        public float pitch;

        public Vector3 standardLookAtDirection = new Vector3(0,0,-1);
        public Vector3 standardUpDirection = new Vector3(0,1,0);

        public Camera(Vector3 position, Vector3 lookAtDirection, Vector3 upDirection, float width, float height, double focalLength, bool orientationLock, float yaw, float pitch)
        {
            this.position = position;
            this.lookAtDirection = lookAtDirection;
            this.upDirection = upDirection;
            this.width = width;
            this.height = height;
            this.focalLength = focalLength;
            this.orientationLock = orientationLock;
            this.yaw = yaw;
            this.pitch = pitch;
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
            if (position[0] >= 0 && position[2] >= 0)
            {
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

    // possible idea: make abstract Ray class, then two classes lightRay and cameraRay. (shaderRay / mirrorRay?)
    // Then have every intersection a Ray field, and every (geometry) primitive a list of closestLightIntersections, and possibly also store the closestCameraIntersection 

    public class Intersection
    {
        public Vector3 intersectionPoint;
        public GeometryPrimitive intersectedPrimitive;
        public double distanceToStartingPoint;
        // GeometryPrimitive nearestPrimitive;
        public Vector3 surfaceNormal;
        public Ray ray;
        internal Color3? textureColor;

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
        public abstract Color3 GetTextureColor(Vector3 pixelPosition);
        public Vector3 position;
        public Color3 diffuseColor;
        public Color3 specularColor;
        public bool mirrorValue;
        public AnyBitmap texture;

        // this field is currently not used, the closestlightintersection is just calculated when needed. But with some refactoring this can probably be extracted fairly cheaply from the "main" loop. This would be in the form of an object / light intersection matrix, not this array.
        public List<Intersection> closestLightIntersections;


        protected GeometryPrimitive(Vector3 position, Color3 diffuseColor, Color3 specularColor, bool mirrorValue, List<Intersection> closestLightIntersections, AnyBitmap texture)
        {
            this.diffuseColor = diffuseColor;
            this.position = position;
            this.diffuseColor = diffuseColor;
            this.specularColor = specularColor;
            this.mirrorValue = mirrorValue;
            this.closestLightIntersections = closestLightIntersections;
            this.texture = texture;
        }
    }

    public class Triangle : GeometryPrimitive
    {
        Vector3 normal;
        float width;
        float height;
        public AnyBitmap texture;

        public Triangle(Vector3 position, Color3 diffuseColor, Color3 specularColor, float width, float height, AnyBitmap texture, bool mirrorValue, List<Intersection> closestIntersections)
            : base(position, diffuseColor, specularColor, mirrorValue, closestIntersections, texture)
        {
            this.width = width;
            this.height = height;
            this.texture = texture;
        }

        public override Intersection? Intersect(Ray ray)
        {
            // Driehoekdefinitie via drie hoekpunten
            Vector3 vertex0 = position;
            Vector3 vertex1 = position + new Vector3(width, 0, 0);   
            Vector3 vertex2 = position + new Vector3(0, 0, height);  

            Vector3 edgeA = vertex1 - vertex0;  
            Vector3 edgeB = vertex2 - vertex0;  

            // Bereken de vector loodrecht op de straal en edgeB
            Vector3 pVec = Vector3.Cross(ray.normal, edgeB);

            float det = Vector3.Dot(edgeA, pVec);
            if (Math.Abs(det) < 1e-6f)
                return null;

            float invDet = 1.0f / det;

            // Vector vanaf vertex0 naar beginpunt van de straal
            Vector3 tVec = ray.startingPosition - vertex0;

            // ligt het punt binnen de driehoek?
            float u = Vector3.Dot(tVec, pVec) * invDet;
            if (u < 0.0f || u > 1.0f)
                return null; 
            Vector3 qVec = Vector3.Cross(tVec, edgeA);
            float v = Vector3.Dot(ray.normal, qVec) * invDet;
            if (v < 0.0f || u + v > 1.0f)
                return null; 

            // Bereken t: afstand langs de straal naar het snijpunt
            float t = Vector3.Dot(edgeB, qVec) * invDet;

            if (t > 1e-6f)
            {
                Vector3 intersectionPoint = ray.startingPosition + t * ray.normal;
                Vector3 surfaceNormal = Vector3.Normalize(Vector3.Cross(edgeA, edgeB));
                Color3? textureCol = texture != null ? GetTextureColor(intersectionPoint) : null;

                return new Intersection(intersectionPoint, this, t, surfaceNormal, ray)
                {
                    textureColor = textureCol
                };
            }

            return null; 
        }

        public override Color3 GetTextureColor(Vector3 point)
        {
            Vector3 v0 = position;
            Vector3 v1 = position + new Vector3(width, 0, 0);
            Vector3 v2 = position + new Vector3(0, 0, height);

            Vector3 edge0 = v1 - v0;
            Vector3 edge1 = v2 - v0;
            Vector3 vp = point - v0;

            float d00 = Vector3.Dot(edge0, edge0);
            float d01 = Vector3.Dot(edge0, edge1);
            float d11 = Vector3.Dot(edge1, edge1);
            float d20 = Vector3.Dot(vp, edge0);
            float d21 = Vector3.Dot(vp, edge1);

            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            float texU = u * 0 + v * 1 + w * 0;
            float texV = u * 0 + v * 0 + w * 1;

            int texX = Math.Clamp((int)(texU * texture.Width), 0, texture.Width - 1);
            int texY = Math.Clamp((int)(texV * texture.Height), 0, texture.Height - 1);

            Color pixelColor = texture.GetPixel(texX, texY);
            return new Color3(pixelColor.R / 255.0f, pixelColor.G / 255.0f, pixelColor.B / 255.0f);
        }

        public override void DebugDraw(Surface screen)
        {
            Vector3 vertex0 = position;
            Vector3 vertex1 = position + new Vector3(width, 0, 0);
            Vector3 vertex2 = position + new Vector3(0, 0, height);

            screen.Line((int)vertex0.X, (int)vertex0.Z, (int)vertex1.X, (int)vertex1.Z, diffuseColor);
            screen.Line((int)vertex1.X, (int)vertex1.Z, (int)vertex2.X, (int)vertex2.Z, diffuseColor);
            screen.Line((int)vertex2.X, (int)vertex2.Z, (int)vertex0.X, (int)vertex0.Z, diffuseColor);
        }
    }

    public class Plane : GeometryPrimitive
    {
        Vector3 normal;
        float width;
        float height;

        public AnyBitmap texture;

        public Plane(Vector3 position, Color3 diffuseColor, Color3 specularColor, Vector3 normal, float width, float height, AnyBitmap texture, bool mirrorValue, List<Intersection> closestIntersections)
            : base(position, diffuseColor, specularColor, mirrorValue, closestIntersections, texture)
        {
            this.normal = normal;
            this.width = width;
            this.height = height;
            this.texture = texture;
        }

        public override Color3 GetTextureColor(Vector3 point)
        {
            // Zelfde u/v-basis als in Intersect en DebugDraw
            Vector3 u = Vector3.Cross(this.normal, new Vector3(0, 1, 0));
            if (u.LengthSquared < 1e-6f) u = Vector3.Cross(this.normal, new Vector3(1, 0, 0));
            u = Vector3.Normalize(u);
            Vector3 v = Vector3.Normalize(Vector3.Cross(this.normal, u));

            Vector3 localVector = point - position;
            float uCoord = 0.5f + Vector3.Dot(localVector, u) / width;   // van 0 tot 1
            float vCoord = 0.5f + Vector3.Dot(localVector, v) / height;

            int texX = Math.Clamp((int)(uCoord * texture.Width), 0, texture.Width - 1);
            int texY = Math.Clamp((int)(vCoord * texture.Height), 0, texture.Height - 1);

            Color pixelColor = texture.GetPixel(texX, texY);
            return new Color3(pixelColor.R / 255.0f, pixelColor.G / 255.0f, pixelColor.B / 255.0f);
        }

        public override Intersection? Intersect(Ray ray)
        {
            // inprduct berekenen met de straal en normaal vector
            double denom = Vector3.Dot(ray.normal, this.normal);
            if (Math.Abs(denom) < 0) return null;

            // afstand t zoals deze staat beschreven in de pwp
            double t = Vector3.Dot(position - ray.startingPosition, this.normal) / denom;
            if (t < 0) return null;

            Vector3 intersectionPoint = ray.startingPosition + ray.normal * (float)t;

            // basisvectoren berekenen
            Vector3 u = Vector3.Cross(this.normal, new Vector3(0, 1, 0));
            float uLength = (float)Math.Sqrt(u.X * u.X + u.Y * u.Y + u.Z * u.Z);
            if (uLength < 1e-6f) // dit werkt beter bij float ofzo
                u = Vector3.Cross(this.normal, new Vector3(1, 0, 0));
            u = Vector3.Normalize(u);

            Vector3 v = Vector3.Normalize(Vector3.Cross(this.normal, u));

            // afstand naar snijpunt
            Vector3 localVector = intersectionPoint - position;
            float uDistance = Vector3.Dot(localVector, u);
            float vDistance = Vector3.Dot(localVector, v);

            if (width != 0 && height != 0)
            {
                // checken of snijpunt binnen vlak valt
                if (Math.Abs(uDistance) > width / 2f || Math.Abs(vDistance) > height / 2f)
                    return null; // Buiten het vlak
            }

                Vector3 surfaceNormal = this.normal;
                return new Intersection(intersectionPoint, this, t, surfaceNormal, ray);

        }

        public override void DebugDraw(Surface screen)
        {
            // Basisvectoren berekenen (zelfde als in Intersect)
            Vector3 u = Vector3.Cross(this.normal, new Vector3(0, 1, 0));
            float uLength = (float)Math.Sqrt(u.X * u.X + u.Y * u.Y + u.Z * u.Z);
            if (uLength < 1e-6f)
                u = Vector3.Cross(this.normal, new Vector3(1, 0, 0));
            u = Vector3.Normalize(u);

            Vector3 v = Vector3.Normalize(Vector3.Cross(this.normal, u));

            // Hoeken van het vlak bepalen
            Vector3 topLeft = position + (width / 2f) * u + (height / 2f) * v;
            Vector3 topRight = position - (width / 2f) * u + (height / 2f) * v;
            Vector3 bottomLeft = position + (width / 2f) * u - (height / 2f) * v;
            Vector3 bottomRight = position - (width / 2f) * u - (height / 2f) * v;

            // Lijnen tekenen die het vlak aangeven 
            screen.Line((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Z), (int)Math.Round(topRight.X), (int)Math.Round(topRight.Z), diffuseColor);
            screen.Line((int)Math.Round(topRight.X), (int)Math.Round(topRight.Z), (int)Math.Round(bottomRight.X), (int)Math.Round(bottomRight.Z), diffuseColor);
            screen.Line((int)Math.Round(bottomRight.X), (int)Math.Round(bottomRight.Z), (int)Math.Round(bottomLeft.X), (int)Math.Round(bottomLeft.Z), diffuseColor);
            screen.Line((int)Math.Round(bottomLeft.X), (int)Math.Round(bottomLeft.Z), (int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Z), diffuseColor);
        }
    }

    public class Sphere : GeometryPrimitive
    {
        public double radius;
        public AnyBitmap texture; // Voeg een texture toe

        public Sphere(Vector3 position, double radius, AnyBitmap texture, Color3 diffuseColor, Color3 specularColor, bool mirrorValue, List<Intersection> closestIntersections)
            : base(position, diffuseColor, specularColor, mirrorValue, closestIntersections, texture)
        {
            this.radius = radius;
            this.texture = texture;
        }

        public override Intersection? Intersect(Ray ray)
        {
            Vector3 differenceVector = ray.startingPosition - position;

            double quadraticTerm = Vector3.Dot(ray.normal, ray.normal);
            double linearTerm = 2 * Vector3.Dot(ray.normal, differenceVector);
            double constantTerm = Vector3.Dot(differenceVector, differenceVector) - (double)(radius * radius);

            double? intersectionDistance = null;
            double discriminant = linearTerm * linearTerm - 4 * quadraticTerm * constantTerm;
            // Console.WriteLine(discriminant);
            if (discriminant < 0)
            {
                return null;
            }
            else
            {
                double rootOne = (-1 * linearTerm + Math.Sqrt(discriminant)) / (2 * quadraticTerm);
                double rootTwo = (-1 * linearTerm - Math.Sqrt(discriminant)) / (2 * quadraticTerm);
                if (rootOne >= 0 && rootTwo >= 0)
                {
                    intersectionDistance = Math.Min(rootOne, rootTwo);
                }
                else if (rootOne >= 0 && rootTwo <= 0)
                {
                    intersectionDistance = rootOne;
                }
                else if (rootOne <= 0 && rootTwo >= 0)
                {
                    intersectionDistance = rootTwo;
                }
            }
            if (intersectionDistance == null)
            {
                return null;
            }

            Vector3 intersectionPoint = ray.normal * (float)intersectionDistance + ray.startingPosition;

            Vector3 pLocal = intersectionPoint - position;
            pLocal = Vector3.Normalize(pLocal);

            float u = 0.5f + (float)(Math.Atan2(pLocal.Z, pLocal.X) / (2 * Math.PI));
            float v = 0.5f - (float)(Math.Asin(pLocal.Y) / Math.PI);

            int texX = (int)(u * (texture.Width - 1));
            int texY = (int)(v * (texture.Height - 1));

            Color pixelColor = texture.GetPixel(texX, texY);

            // Converteer pixelColor naar Color3 (normale 0-1 waarden)
            Color3 colorFromTexture = new Color3(
                pixelColor.R / 255.0f,
                pixelColor.G / 255.0f,
                pixelColor.B / 255.0f
            );

            Vector3 surfaceNormal = position - intersectionPoint;
            surfaceNormal.Normalize();

            return new Intersection(intersectionPoint, this, intersectionDistance.Value, surfaceNormal, ray);

        }

        public override Color3 GetTextureColor(Vector3 point)
        {
            Vector3 pLocal = Vector3.Normalize(point - position);

            float u = 0.5f + (float)(Math.Atan2(pLocal.Z, pLocal.X) / (2 * Math.PI));
            float v = 0.5f - (float)(Math.Asin(pLocal.Y) / Math.PI);

            int texX = (int)(u * (texture.Width - 1));
            int texY = (int)(v * (texture.Height - 1));

            Color pixelColor = texture.GetPixel(texX, texY);

            return new Color3(pixelColor.R / 255.0f, pixelColor.G / 255.0f, pixelColor.B / 255.0f);
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

        Color3 Trace(Intersection mirrorIntersection, int NOB)
        {
            int MAXBOUNCES = 5;
            if (NOB >= MAXBOUNCES)
            {
                return new Color3(0,0,0);
            }

            Vector3 reflectedVectorNormal = (mirrorIntersection.ray.normal - 2 * Vector3.Dot(mirrorIntersection.ray.normal, mirrorIntersection.surfaceNormal) * mirrorIntersection.surfaceNormal).Normalized();

            List<Intersection> intersectionArray = new List<Intersection>();

            foreach (GeometryPrimitive primitive in scene.primitives)
            {
                Intersection? intersectionResult = primitive.Intersect(new Ray(reflectedVectorNormal, mirrorIntersection.intersectionPoint));
                if (intersectionResult != null)
                {
                    intersectionArray.Add(intersectionResult);
                }
            }

            if (intersectionArray.Count() != 0)
            {
                Intersection closestIntersection = intersectionArray
                    .OrderBy(i => i.distanceToStartingPoint)
                    .First();

                if (closestIntersection.intersectedPrimitive.mirrorValue == true)
                {
                    return Trace(closestIntersection, NOB + 1);
                }
                else
                {
                    return DeterminePixelColor(closestIntersection);
                }
            }
            else
            {
                return new Color3(0, 0, 0);
            }
        

        }

        Color3 DeterminePixelColor(Intersection closestPrimaryRayIntersection)
        {
            Color3 baseColor;
            if (closestPrimaryRayIntersection.intersectedPrimitive.texture != null)
            {
                baseColor = closestPrimaryRayIntersection.intersectedPrimitive.GetTextureColor(closestPrimaryRayIntersection.intersectionPoint);
            }
            else
            {
                baseColor = closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor;
            }

            // Ambient lighting component
            Color3 pixelColor = new Color3(
                baseColor.R * scene.ambientRadiance.R,
                baseColor.G * scene.ambientRadiance.G,
                baseColor.B * scene.ambientRadiance.B);

            foreach (LightSource lightSource in scene.lightSources)
            {
                List<Intersection> lightRayIntersectionArray = new List<Intersection>();
                Vector3 lightRayNormal = (closestPrimaryRayIntersection.intersectionPoint - lightSource.position).Normalized();

                Ray lightRay = new Ray(lightRayNormal, lightSource.position);
                foreach (GeometryPrimitive primitive in scene.primitives)
                {
                    Intersection? intersectionResult = primitive.Intersect(lightRay);
                    if (intersectionResult != null)
                    {
                        lightRayIntersectionArray.Add(intersectionResult);
                    }
                }

                if (lightRayIntersectionArray.Count != 0)
                {
                    // Console.WriteLine(lightRayIntersectionArray.Count);
                    Intersection closestLightRayIntersection = lightRayIntersectionArray
                        .OrderBy(i => i.distanceToStartingPoint)
                        .First();

                    if ((closestLightRayIntersection.intersectionPoint - closestPrimaryRayIntersection.intersectionPoint).Length > 0.1)
                    {
                        continue;
                    }

                    Vector3 lightRayDiff = closestLightRayIntersection.intersectionPoint - closestLightRayIntersection.ray.startingPosition;
                    Vector3 primaryRayDiff = closestPrimaryRayIntersection.intersectionPoint - closestPrimaryRayIntersection.ray.startingPosition;
                    if (Vector3.Dot(lightRayDiff, closestPrimaryRayIntersection.surfaceNormal) * Vector3.Dot(primaryRayDiff, closestPrimaryRayIntersection.surfaceNormal) < 0)
                    {
                        continue;
                    }


                    // shading logic                               
                    float diffuseReflectionRatio = Math.Max(0, Vector3.Dot(lightRay.normal, closestPrimaryRayIntersection.surfaceNormal)) / (float)Math.Pow(closestLightRayIntersection.distanceToStartingPoint, 2);

                    Vector3 diffuseContribution = new Vector3(
                        diffuseReflectionRatio * lightSource.color.R * baseColor.R,
                        diffuseReflectionRatio * lightSource.color.G * baseColor.G,
                        diffuseReflectionRatio * lightSource.color.B * baseColor.B);

                    Vector3 lightVector = lightRay.normal * (float)closestLightRayIntersection.distanceToStartingPoint;
                    Vector3 reflectedVectorNormal = (lightVector - 2 * Vector3.Dot(lightVector, closestLightRayIntersection.surfaceNormal) * closestLightRayIntersection.surfaceNormal).Normalized();

                    Vector3 viewVectorNormal = (closestPrimaryRayIntersection.ray.normal * (float)closestPrimaryRayIntersection.distanceToStartingPoint).Normalized();

                    int specularity = 50;
                    float specularReflectionRatio = (float)Math.Pow(Math.Max(0, -1 * Vector3.Dot(reflectedVectorNormal, viewVectorNormal)), specularity) / (float)Math.Pow(closestLightRayIntersection.distanceToStartingPoint, 2);

                    Vector3 specularContribution = new Vector3(
                        specularReflectionRatio * lightSource.color.R * closestPrimaryRayIntersection.intersectedPrimitive.specularColor.R,
                        specularReflectionRatio * lightSource.color.G * closestPrimaryRayIntersection.intersectedPrimitive.specularColor.G,
                        specularReflectionRatio * lightSource.color.B * closestPrimaryRayIntersection.intersectedPrimitive.specularColor.B
                    );


                    pixelColor = new Color3(
                        pixelColor.R + diffuseContribution[0] + specularContribution[0],
                        pixelColor.G + diffuseContribution[1] + specularContribution[1],
                        pixelColor.B + diffuseContribution[2] + specularContribution[2]
                    );

                }


            }

            return pixelColor;
        }

        public void Render(bool debugMode)
        {
            if (debugMode == true)
            {
                Vector3 focalPoint = camera.position - camera.lookAtDirection * (float)camera.focalLength;

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

                                // This should make the lightrays not draw when inside a sphere, but it doesnt work
                                // Vector3 lightRayDiff = closestLightRayIntersection.intersectionPoint - closestLightRayIntersection.ray.startingPosition;
                                // Vector3 primaryRayDiff = closestIntersection.intersectionPoint - closestIntersection.ray.startingPosition;
                                // if (Vector3.Dot(lightRayDiff, closestIntersection.surfaceNormal) * Vector3.Dot(primaryRayDiff, closestIntersection.surfaceNormal) < 0)
                                // {
                                //     continue;
                                // }

                                screen.Line((int)Math.Round(lightSource.position[0]),
                                            (int)Math.Round(lightSource.position[2]),
                                            (int)Math.Round(closestLightRayIntersection.intersectionPoint[0]),
                                            (int)Math.Round(closestLightRayIntersection.intersectionPoint[2]),
                                            new Color3(1, 1, 1));
                            }
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

                // Parallel.For(0, screen.height + 1, heightPixel =>
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
                            // Bepaal de juiste diffuse kleur: textuurkleur als het een Sphere met texture is, anders gewone diffuseColor

                            // Aanroep texture sphere
                            //Color3 baseColor = closestPrimaryRayIntersection.intersectedPrimitive is Sphere texturedSphere && texturedSphere.texture != null
                            //? texturedSphere.GetTextureColor(closestPrimaryRayIntersection.intersectionPoint)
                            //: closestPrimaryRayIntersection.intersectedPrimitive.diffuseColor;

                            Color3 pixelColor;
                            if (closestPrimaryRayIntersection.intersectedPrimitive.mirrorValue == true)
                            {
                                pixelColor = Trace(closestPrimaryRayIntersection, 0);
                            }
                            else
                            {
                                pixelColor = DeterminePixelColor(closestPrimaryRayIntersection);
                            }

                            screen.Plot(widthPixel, heightPixel, pixelColor);
                        }
                    });
                }

            }

        }
    }

    public class Texture
    {
        public AnyBitmap bitmap;

        // Constructor: laadt een afbeelding van schijf
        public Texture(string Marmer)
        {
            bitmap = new AnyBitmap(Marmer);
        }

        // Haalt kleur op van gegeven UV-coordinaten
        public Vector3 GetColor(float u, float v)
        {
            // Clamp zorgt dat u en v binnen [0, 1] blijven
            u = Math.Clamp(u, 0, 1);
            v = Math.Clamp(v, 0, 1);

            // Bereken pixelpositie in bitmap
            int x = (int)(u * (bitmap.Width - 1));
            int y = (int)((1 - v) * (bitmap.Height - 1)); // y = 0 is bovenaan

            // Haal kleur op van die pixel
            Color color = bitmap.GetPixel(x, y);

            // Geef kleur terug als Vector3 (0�1 schaal)
            return new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
        }
    }

    class MyApplication
    {
        // member variables
        public Surface screen;
        private readonly Stopwatch timer = new();
        // constructor
        public Camera camera;
        public SceneGeometry scene;
        public RayTracer rayTracer;

        public MyApplication(Surface screen)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            this.screen = screen;


            AnyBitmap marmerTexture = new AnyBitmap("Marmer.png");
            AnyBitmap baksteenTexture = new AnyBitmap("Bakstenen.png");

            camera = new Camera(
                new Vector3(200, 0, 2000),
                new Vector3(0, 0, -1),
                new Vector3(0, 1, 0),
                50,
                10,
                56.0,
                true,
                0,
                0);


            scene = new SceneGeometry(
            [
                new Sphere(
                    new Vector3(100, 0, 300),
                    40,
                    marmerTexture,
                    new Color3(0,1,0),
                    new Color3(1,1,1),
                    true, []),
                new Sphere(
                    new Vector3(600, 0, 300),
                    100, new AnyBitmap("Marmer.png"),
                    new Color3(1,0,0),
                    new Color3(1,1,1),
                    false, []),
                new Plane(
                    new Vector3(20, 0, 350),
                    new Color3(1,0,0),
                    new Color3(1,1,1),
                    new Vector3(1, 0, 0),
                    150,
                    150, 
                    null,
                    false, []),
                new Triangle(
                    new Vector3(50, 20, 100),
                    new Color3(1, 0, 0), 
                    new Color3(1, 1, 1),
                    400,
                    500,
                    baksteenTexture,
                    false,[]),
            ],
            [
                new LightSource(new Vector3(600, 500, 500), new Color3(1,1,1) * 200000 ),
                new LightSource(new Vector3(300, 500, 800), new Color3(1,1,1) * 200000 )
            ], new Color3((float)0, (float)0, (float)0));
            rayTracer = new RayTracer(scene, camera, screen);
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

        public bool debugMode = true;
        public bool debugData = false;
        double PI = 3.1415926535897932384626433832795028;
        
        public void Tick()
        {
            timer.Restart();
            screen.Clear(new Color3((float)0.2, (float)0.2, (float)0.2));

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

            if (debugData == true)
            {
                screen.PrintOutlined(timeString, 2, 2, Color4.White);

                double fieldOfView = 2 * Math.Atan(screen.width / (2 * camera.focalLength)) * 180 / PI;
                screen.PrintOutlined("field of view:" + double.Round(fieldOfView).ToString() + "degrees", 300, 2, Color4.White);
                screen.PrintOutlined("position:" + camera.position[0].ToString() + "," + camera.position[1].ToString() + "," + camera.position[2].ToString(), 2, 25, Color4.White);
                screen.PrintOutlined("yaw: " + camera.yaw.ToString() + " " + "pitch: " + camera.pitch.ToString(), 2, 50, Color4.White);
            }
        }


    }
}