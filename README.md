# AI XR TextTo3D

A Unity project integrating a local FastAPI backend for AI-driven 3D model generation from text prompts. The repo combines Unity frontend UI, GLTFast import support, and a Python-based TRELLIS backend to generate, serve, and load GLB models in the Unity editor.

## Project Overview

- Unity frontend with UI and XR-ready package support.
- Backend process launched from Unity at runtime via `BackendManager`.
- FastAPI service in `Assets/StreamingAssets/UnityBackend/main.py`.
- Generated models are written to `Assets/StreamingAssets/UnityBackend/generated/output.glb`.
- `APIManager` sends text prompts and quality choices to the backend.

## Key Components

- `Assets/Scripts/BackendManager.cs` - starts/stops the Python backend and checks service readiness.
- `Assets/Scripts/APIManager.cs` - sends prompts to `/generate`, polls `/progress`, and shows generation status.
- `Assets/Scripts/ModelLoader.cs` - imports generated GLB output into the Unity scene.
- `Assets/StreamingAssets/UnityBackend/main.py` - FastAPI backend entrypoint.
- `Assets/StreamingAssets/UnityBackend/requirements.txt` - Python dependencies for the backend.

## Requirements

- Unity 2022.3.x (project assets and packages are configured for Unity 2022.3).
- Python 3.10 installed.
- Local Python virtual environment at `backend/venv/` with dependencies installed.
- Optional: CUDA GPU support for PyTorch if available.

## Setup

1. Open the project in Unity by selecting the root folder or `AI_XR_TextTo3D.sln`.
2. Create a Python virtual environment under the repo root:
   ```bash
   python -m venv backend/venv
   ```
3. Activate the venv and install backend dependencies:
   - Windows (PowerShell):
     ```powershell
     .\backend\venv\Scripts\Activate.ps1
     pip install -r "Assets/StreamingAssets/UnityBackend/requirements.txt"
     ```
   - Windows (cmd):
     ```cmd
     backend\venv\Scripts\activate.bat
     pip install -r "Assets\StreamingAssets\UnityBackend\requirements.txt"
     ```
4. Confirm `backend/venv/Scripts/python.exe` exists and `Assets/StreamingAssets/UnityBackend/main.py` is present.

## Usage

1. In Unity, open the scene containing the generation UI.
2. Enter a text prompt and select generation quality.
3. Click the generate button to start the backend model creation flow.
4. The backend exposes:
   - `/generate` to start generation
   - `/progress` for progress updates
   - `/result` for final model information
   - `/models/output.glb` as the generated GLB asset
5. The generated GLB is loaded via `ModelLoader` and displayed in the scene.

## Notes

- The backend uses FastAPI and runs on `http://127.0.0.1:8000`.
- The Unity project includes `com.unity.cloud.gltfast` for GLB import workflows.
- If the backend fails to start, check that Python is installed and the virtual environment is created correctly.

## Repo Structure

- `Assets/` - Unity project assets and scripts.
- `Assets/StreamingAssets/UnityBackend/` - backend FastAPI code and model generation resources.
- `backend/venv/` - expected local Python virtual environment.
- `Packages/manifest.json` - Unity package dependencies.
- `AI_XR_TextTo3D.sln` - Unity solution file.

## Contribution

Add issues or improvements by editing Unity scripts, backend generation logic, or documentation for setup and usage.
