Team members: (names and student IDs)
* Boris van Boxtel 1210556
* Isabel Spillebeen 7531583

Tick the boxes below for the implemented features. Add a brief note only if necessary, e.g., if it's only partially working, or how to turn it on.

Formalities:
[x] This readme.txt
[x] Cleaned (no obj/bin folders)

Minimum requirements implemented:
[x] Camera: position and orientation controls, field of view in degrees
Controls:
    - WS for movement in direction of camera.normal
    - AD for movement in direction of camera.leftdirection
    - FR for fov changes
    - N to toggle debug text
    - M to toggle debug mode
    - O to toggle the orientationlock, horizontal movement is done with horizontal movement of the camera, vertical movement idem
[x] Primitives: plane, sphere
[x] Lights: at least 2 point lights, additive contribution, shadows without "acne"
[x] Diffuse shading: (N.L), distance attenuation
[x] Phong shading: (R.V) or (N.H), exponent
[x] Diffuse color texture: only required on the plane primitive, image or procedural, (u,v) texture coordinates
[x] Mirror reflection: recursive
[x] Debug visualization: sphere primitives, rays (primary, shadow, reflected, refracted)

Bonus features implemented:
[x] Triangle primitives: must use the algorithm from the lectures, single triangles or meshes
[ ] Interpolated normals: only required on triangle primitives, 3 different vertex normals must be specified
[ ] Spot lights: smooth falloff optional
[ ] Glossy reflections: not only of light sources but of other objects
[ ] Anti-aliasing
[x] Parallelized
Method: binnensteLoop een parallel for
[x] Textures: on all implemented primitives
[ ] Bump or normal mapping: on all implemented primitives
[ ] Environment mapping: sphere or cube map, without intersecting actual sphere/cube/triangle primitives
[ ] Refraction: also requires a reflected ray at every refractive surface, recursive
[ ] Area lights: soft shadows
[ ] Acceleration structure: bounding box or hierarchy, scene with 5000+ primitives
Performance comparison: ... (provide one measurement of speed/time with and without the acceleration structure)
[ ] GPU implementation
Method: ... (for example: fragment shader, compute shader, ILGPU, or CUDA)

Notes:
-  The planes can be infinite by setting either size to 0.

