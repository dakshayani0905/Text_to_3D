from pathlib import Path

MODEL_NAME = "models--microsoft--TRELLIS-text-xlarge"

def is_model_downloaded():

    cache_dir = Path.home() / ".cache" / "huggingface" / "hub"

    model_dir = cache_dir / MODEL_NAME

    return model_dir.exists()