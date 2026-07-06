from typing import *
import torch
import math
from torch.nn.functional import scaled_dot_product_attention as sdpa
from . import DEBUG, BACKEND

# FORCE disable flash_attn completely
BACKEND = 'xformers'

try:
    import xformers.ops as xops
    XFORMERS_AVAILABLE = True
except:
    XFORMERS_AVAILABLE = False

__all__ = [
    'scaled_dot_product_attention',
]


def _naive_sdpa(q, k, v):
    q = q.permute(0, 2, 1, 3)
    k = k.permute(0, 2, 1, 3)
    v = v.permute(0, 2, 1, 3)

    scale_factor = 1 / math.sqrt(q.size(-1))

    attn_weight = q @ k.transpose(-2, -1) * scale_factor
    attn_weight = torch.softmax(attn_weight, dim=-1)

    out = attn_weight @ v
    out = out.permute(0, 2, 1, 3)

    return out


@overload
def scaled_dot_product_attention(qkv: torch.Tensor) -> torch.Tensor:
    ...


@overload
def scaled_dot_product_attention(q: torch.Tensor, kv: torch.Tensor) -> torch.Tensor:
    ...


@overload
def scaled_dot_product_attention(q: torch.Tensor, k: torch.Tensor, v: torch.Tensor) -> torch.Tensor:
    ...


def scaled_dot_product_attention(*args, **kwargs):

    arg_names_dict = {
        1: ['qkv'],
        2: ['q', 'kv'],
        3: ['q', 'k', 'v']
    }

    num_all_args = len(args) + len(kwargs)

    assert num_all_args in arg_names_dict

    if num_all_args == 1:
        qkv = args[0] if len(args) > 0 else kwargs['qkv']
        q, k, v = qkv.unbind(dim=2)

    elif num_all_args == 2:
        q = args[0] if len(args) > 0 else kwargs['q']
        kv = args[1] if len(args) > 1 else kwargs['kv']
        k, v = kv.unbind(dim=2)

    elif num_all_args == 3:
        q = args[0] if len(args) > 0 else kwargs['q']
        k = args[1] if len(args) > 1 else kwargs['k']
        v = args[2] if len(args) > 2 else kwargs['v']

    # TRY XFORMERS FIRST
    if XFORMERS_AVAILABLE:
        try:
            out = xops.memory_efficient_attention(q, k, v)
            return out
        except:
            pass

    # FALLBACK TO PYTORCH SDPA
    try:
        q = q.permute(0, 2, 1, 3)
        k = k.permute(0, 2, 1, 3)
        v = v.permute(0, 2, 1, 3)

        out = sdpa(
            q,
            k,
            v,
            attn_mask=None,
            dropout_p=0.0,
            is_causal=False
        )

        out = out.permute(0, 2, 1, 3)

        return out

    except:
        return _naive_sdpa(q, k, v)