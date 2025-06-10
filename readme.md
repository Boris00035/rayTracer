### **Team Members**  
- Boris van Boxtel (1210556)  
- Isabel Spillebeen (7531583)  

### **Implemented Features**  
*(Tick the boxes below. Add notes if partially working.)*  

#### **Formalities**  
- [x] This `readme.txt`  
- [x] Cleaned (no `obj/bin` folders)  

#### **Minimum Requirements**  
- [x] **Camera**: Position/orientation controls, field of view (degrees)  
  **Controls**:  
  - `WS`: Move along camera normal  
  - `AD`: Move along camera left direction  
  - `FR`: Adjust FOV  
  - `N`: Toggle debug text  
  - `M`: Toggle debug mode  
  - `O`: Toggle orientation lock (horizontal/vertical movement follows camera)  

- [x] **Primitives**: Plane, sphere  
- [x] **Lights**: ≥2 point lights, additive contribution, shadows (no "acne")  
- [x] **Diffuse shading**: `(N.L)` + distance attenuation  
- [x] **Phong shading**: `(R.V)` or `(N.H)` + exponent  
- [x] **Diffuse texture**: Plane only (image/procedural, `(u,v)` coords)  
- [x] **Mirror reflection**: Recursive  
- [x] **Debug visualization**: Sphere primitives, rays (primary/shadow/reflected/refracted)  

#### **Bonus Features**  
- [x] **Triangle primitives**: Lecture’s algorithm (single/meshes)  
- [ ] **Interpolated normals**: Triangle primitives only (3 vertex normals)  
- [ ] **Spot lights**: Smooth falloff optional  
- [ ] **Glossy reflections**: Objects (not just lights)  
- [ ] **Anti-aliasing**  
- [x] **Parallelized**  
  *(Method: Inner loop as `Parallel.For`)*  
- [x] **Textures**: All primitives  
- [ ] **Bump/normal mapping**: All primitives  
- [ ] **Environment mapping**: Sphere/cube map (no intersections)  
- [ ] **Refraction**: Reflected ray at refractive surfaces (recursive)  
- [ ] **Area lights**: Soft shadows  
- [ ] **Acceleration structure**: Bounding box/hierarchy (5000+ primitives)  
  *(Performance comparison: N/A)*  
- [ ] **GPU implementation**  
  *(Method: N/A)*  

#### **Notes**  
- Planes can be infinite (set size to `0`).
