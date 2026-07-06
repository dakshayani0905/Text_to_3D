import torch

def validate_gpu():

    if not torch.cuda.is_available():

        return {
            "gpu": False,
            "message": "CUDA GPU Required"
        }

    gpu_name = torch.cuda.get_device_name(0)

    return {
        "gpu": True,
        "message": gpu_name
    }