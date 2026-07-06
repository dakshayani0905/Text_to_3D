import os
import sys

os.environ['SPCONV_ALGO'] = 'native'

from trellis.pipelines import TrellisTextTo3DPipeline
from trellis.utils import postprocessing_utils

prompt = sys.argv[1]

pipeline = TrellisTextTo3DPipeline.from_pretrained(
    "microsoft/TRELLIS-text-xlarge"
)

pipeline.cuda()

outputs = pipeline.run(
    prompt,
    seed=1,
)
print("STARTING TO_GLB")
glb = postprocessing_utils.to_glb(
    outputs['gaussian'][0],
    outputs['mesh'][0],
    simplify=0.95,
    fill_holes=False,
    texture_size=2048,
)
print("TO_GLB FINISHED")
glb.export("output.glb")

print("DONE")
