from fastapi import FastAPI
from pydantic import BaseModel
from fastapi.staticfiles import StaticFiles
import os
import threading
import torch
from progress import progress_status
from trellis_generator import generate_model
from model_check import is_model_downloaded
import cancel

app = FastAPI()

generation_result = {
    "status": "idle",
    "file_path": ""
}
progress_status["message"] = "Starting"
progress_status["percent"] = 0

# ==========================================
# GENERATED MODELS FOLDER
# ==========================================

PROJECT_ROOT = os.path.abspath(
    os.path.join(
        os.path.dirname(__file__),
        "..",
        "..",
        ".."
    )
)


GENERATED_DIR = os.path.join(
    PROJECT_ROOT,
    "Assets",
    "Prefabs"
)

os.makedirs(
    GENERATED_DIR,
    exist_ok=True
)

app.mount(
    "/models",
    StaticFiles(directory=GENERATED_DIR),
    name="models"
)

# ==========================================
# REQUEST MODEL
# ==========================================

class PromptData(BaseModel):
    prompt: str
    quality: str

# ==========================================
# BACKGROUND GENERATION
# ==========================================

def run_generation(prompt, quality):

    global generation_result

    try:

        output_path = generate_model(
            prompt,
            quality
        )

        generation_result = {
            "status": "completed",
            "model_url":
                "http://127.0.0.1:8000/models/output.glb",
            "file_path":
                output_path
        }

    except Exception as e:

        import traceback

        traceback.print_exc()

        if str(e) == "Generation Cancelled":

            generation_result = {
                "status": "cancelled"
            }

        else:

            generation_result = {
                "status": "failed",
                "error": str(e)
            }
# ==========================================
# START GENERATION
# ==========================================

@app.post("/generate")
def generate(data: PromptData):

    global generation_result

    cancel.cancel_requested = False

    # RESET PROGRESS AFTER CANCEL
    progress_status["message"] = "Starting Generation"
    progress_status["percent"] = 0

    generation_result = {
        "status": "running",
        "file_path": "",
        "model_url": ""
    }

    thread = threading.Thread(
        target=run_generation,
        args=(
            data.prompt,
            data.quality
        )
    )

    thread.start()

    return {
        "status": "started"
    }

# ==========================================
# PROGRESS
# ==========================================

@app.get("/progress")
def progress():

    return progress_status

# ==========================================
# RESULT
# ==========================================

@app.get("/result")
def result():

    return generation_result
@app.get("/gpu")
def gpu():

    if torch.cuda.is_available():

        return {
            "gpu": True,
            "message": torch.cuda.get_device_name(0)
        }

    return {
        "gpu": False,
        "message": "CUDA GPU Not Found"
    }
@app.post("/cancel")
def cancel_generation():

    global generation_result

    cancel.cancel_requested = True

    generation_result = {
        "status": "cancelled"
    }

    progress_status["message"] = "Generation Cancelled"
    progress_status["percent"] = 0

    return {
        "status": "cancelled"
    }
# ==========================================
# HEALTH CHECK
# ==========================================

@app.get("/")
def root():

    return {
        "status": "running",
        "message": "TRELLIS Backend Running"
    }

# ==========================================
# START SERVER
# ==========================================

if __name__ == "__main__":

    import uvicorn

    uvicorn.run(
        app,
        host="127.0.0.1",
        port=8000
    )