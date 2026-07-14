# TRELLIS Unity Plugin
### AI-Powered Text-to-3D Asset Generation for Unity

Generate high-quality 3D assets directly inside Unity using natural language prompts. This project integrates **Microsoft TRELLIS** with the **Unity Editor** through a **FastAPI backend**, enabling AI-powered text-to-3D generation without leaving the Unity environment.

---

## Features

- Text-to-3D generation using Microsoft TRELLIS
- Custom Unity Editor Tool (`Tools → TRELLIS Generator`)
- Automatic Python backend startup
- Automatic backend shutdown
- GPU detection
- Auto quality selection based on GPU VRAM
- Manual quality modes (Fast, Balanced, Ultra)
- Real-time generation progress
- Generate directly inside Unity
- Automatic GLB export
- Automatic Prefab creation
- Cancel generation support

---

## Demo Workflow

```
User Prompt
      │
      ▼
Unity Editor Window
      │
      ▼
FastAPI Backend
      │
      ▼
TRELLIS Text-to-3D Pipeline
      │
      ▼
Mesh Generation
      │
      ▼
Texture Baking
      │
      ▼
Export output.glb
      │
      ▼
Unity Import
      │
      ▼
Prefab Creation
```

---

# Project Structure

```
AI_XR_TextTo3D
│
├── Assets
│   ├── Editor
│   │   └── TrellisGeneratorEditor.cs
│   │
│   ├── Prefabs
│   │   ├── output.glb
│   │   └── output.prefab
│   │
│   ├── StreamingAssets
│   │   └── UnityBackend
│   │       ├── main.py
│   │       ├── trellis_generator.py
│   │       ├── progress.py
│   │       ├── cancel.py
│   │       ├── model_check.py
│   │       └── trellis/
│   │
│   └── Scripts
│
├── backend
│   └── venv
│
└── Packages
```

---

# Technologies Used

### Unity
- Unity 2022.3 LTS
- C#
- Unity Editor API

### Backend
- Python 3.10
- FastAPI
- Uvicorn

### AI
- Microsoft TRELLIS
- PyTorch
- CUDA

### 3D
- UnityGLTF
- GLB Format

---

# Quality Modes

| Mode | Description |
|------|-------------|
| Auto | Automatically selects the best quality based on GPU VRAM |
| Fast | 1024 texture resolution, optimized for lower-end GPUs |
| Balanced | 2048 texture resolution with balanced performance |
| Ultra | 4096 texture resolution for highest quality output |

---

# Backend API

### Generate Model

```
POST /generate
```

```json
{
    "prompt": "A wooden chair",
    "quality": "Auto"
}
```

---

### Progress

```
GET /progress
```

---

### Result

```
GET /result
```

---

### GPU Check

```
GET /gpu
```

---

### Cancel Generation

```
POST /cancel
```

---

# How It Works

1. Open **Tools → TRELLIS Generator**.
2. Enter a text prompt.
3. Select a quality mode.
4. Click **Generate**.
5. The backend starts automatically.
6. TRELLIS generates the 3D model.
7. The generated model is exported as:

```
Assets/Prefabs/output.glb
```

8. Unity imports the model.
9. A prefab is automatically created.
10. The backend is terminated after completion.

---

# Example Prompt

```
A medieval knight helmet made of polished steel with engraved patterns and realistic proportions.
```

---

# Requirements

- Unity 2022.3 LTS
- Python 3.10
- NVIDIA CUDA GPU
- Windows 10/11

---

# Current Features

- AI-powered Text-to-3D generation
- Automatic backend management
- GPU detection
- Automatic quality selection
- Progress tracking
- GLB export
- Unity integration
- Prefab generation
- Cancel generation

---

# Future Improvements

- Multiple model generation
- Batch generation
- Prompt history
- Texture editing
- Material customization
- Animation support
- One-click Unity Package installation

---

# Author

**Dakshayani K**
**Annie Christian**
**Munendra P**
B.Tech Computer Science Engineering

GITAM University, Visakhapatnam

---

# Acknowledgements

- Microsoft TRELLIS
- Unity Technologies
- FastAPI
- PyTorch
