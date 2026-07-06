from pathlib import Path

def is_model_downloaded():

    cache_dir = Path.home() / ".cache" / "huggingface"

    model_dir = (
        cache_dir
        / "hub"
        / "models--microsoft--TRELLIS-text-xlarge"
    )

    return model_dir.exists()