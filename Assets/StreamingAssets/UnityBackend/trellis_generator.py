import sys
import os
import torch

from progress import progress_status
BASE_DIR = os.path.dirname(os.path.abspath(__file__))

TRELLIS_ROOT = BASE_DIR

if TRELLIS_ROOT not in sys.path:
    sys.path.insert(0, TRELLIS_ROOT)

if TRELLIS_ROOT not in sys.path:
    sys.path.insert(0, TRELLIS_ROOT)
from trellis.pipelines import TrellisTextTo3DPipeline
from trellis.utils import postprocessing_utils

pipeline = None


def load_model():

    global pipeline

    if pipeline is None:

        progress_status["message"] = "Loading TRELLIS Model"
        progress_status["percent"] = 5

        MODEL_NAME = "microsoft/TRELLIS-text-xlarge"

        pipeline = TrellisTextTo3DPipeline.from_pretrained(
            MODEL_NAME
        )

        if not torch.cuda.is_available():
            raise RuntimeError(
                "CUDA GPU is required to run TRELLIS."
            )

        pipeline.cuda()

        print(
            f"Running TRELLIS on CUDA"
        )
def get_quality_settings(quality):

    if quality == "Fast":

        return {
            "simplify": 0.90,
            "texture_size": 1024
        }

    elif quality == "Balanced":

        return {
            "simplify": 0.95,
            "texture_size": 2048
        }

    elif quality == "Ultra":

        return {
            "simplify": 0.98,
            "texture_size": 4096
        }

    else:  # Auto

        vram = (
            torch.cuda.get_device_properties(0)
            .total_memory
            / (1024 ** 3)
        )

        print(
            f"GPU VRAM: {vram:.1f} GB"
        )

        if vram <= 8:

            print(
                "AUTO -> FAST"
            )

            return {
                "simplify": 0.90,
                "texture_size": 1024
            }

        elif vram <= 16:

            print(
                "AUTO -> BALANCED"
            )

            return {
                "simplify": 0.95,
                "texture_size": 2048
            }

        else:

            print(
                "AUTO -> ULTRA"
            )

            return {
                "simplify": 0.98,
                "texture_size": 4096
            }


def generate_model(prompt, quality):

    load_model()

    settings = get_quality_settings(
        quality
    )

    progress_status["message"] = \
        "Preparing Generation"

    progress_status["percent"] = 10

    print(
        "QUALITY:",
        quality
    )

    print(
        "WORKING DIR:",
        os.getcwd()
    )

    progress_status["message"] = \
        "Sampling Structure"

    progress_status["percent"] = 25

    torch.cuda.empty_cache()
    outputs = pipeline.run(
        prompt,
        seed=1
    )

    progress_status["message"] = \
        "Generating Geometry"

    progress_status["percent"] = 50

    print(
        "STARTING TO_GLB"
    )

    progress_status["message"] = \
        "Converting To Mesh"

    progress_status["percent"] = 70
    
    glb = postprocessing_utils.to_glb(
        outputs['gaussian'][0],
        outputs['mesh'][0],
        simplify=settings["simplify"],
        fill_holes=False,
        texture_size=settings["texture_size"]
    )

    print(
        "TO_GLB FINISHED"
    )

    progress_status["message"] = \
        "Baking Textures"

    progress_status["percent"] = 85

    GENERATED_DIR = os.path.join(
        BASE_DIR,
        "generated"
    )

    os.makedirs(
        GENERATED_DIR,
        exist_ok=True
    )

    output_path = os.path.join(
        GENERATED_DIR,
        "output.glb"
    )

    print(
        "EXPORTING TO:",
        os.path.abspath(output_path)
    )

    progress_status["message"] = \
        "Exporting Model"

    progress_status["percent"] = 95
    
    glb.export(output_path)

    del outputs
    del glb

    import gc

    gc.collect()

    torch.cuda.empty_cache()

    progress_status["message"] = "Completed"
    progress_status["percent"] = 100

    return output_path