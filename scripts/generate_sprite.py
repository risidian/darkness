#!/usr/bin/env python3
"""
ComfyUI Automated Pixel Art / Sprite Generator for Darkness Godot Game
Connects to ComfyUI REST API (http://127.0.0.1:8188) and generates assets using FLUX GGUF models.
"""

import sys
import os
import json
import time
import random
import argparse
import urllib.request
import urllib.parse

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

DEFAULT_SERVER = "http://127.0.0.1:8188"

def build_flux_gguf_workflow(prompt_text: str, width: int = 512, height: int = 512, steps: int = 4, seed: int = -1):
    if seed == -1:
        seed = random.randint(1, 1000000000)

    full_prompt = f"pixel art, 16-bit retro game asset, {prompt_text}, clean sharp pixels, isolated on white background"
    
    workflow = {
        "1": {
            "inputs": {
                "unet_name": "flux1-schnell-Q4_K_S.gguf"
            },
            "class_type": "UnetLoaderGGUF"
        },
        "2": {
            "inputs": {
                "clip_name1": "t5xxl_fp8_e4m3fn.safetensors",
                "clip_name2": "clip_l.safetensors",
                "type": "flux"
            },
            "class_type": "DualCLIPLoader"
        },
        "3": {
            "inputs": {
                "vae_name": "ae.safetensors"
            },
            "class_type": "VAELoader"
        },
        "4": {
            "inputs": {
                "text": full_prompt,
                "clip": ["2", 0]
            },
            "class_type": "CLIPTextEncode"
        },
        "5": {
            "inputs": {
                "text": "blurry, 3d render, photo, realistic, noisy, anti-aliased smooth gradients",
                "clip": ["2", 0]
            },
            "class_type": "CLIPTextEncode"
        },
        "6": {
            "inputs": {
                "width": width,
                "height": height,
                "batch_size": 1
            },
            "class_type": "EmptySD3LatentImage"
        },
        "7": {
            "inputs": {
                "seed": seed,
                "steps": steps,
                "cfg": 1.0,
                "sampler_name": "euler",
                "scheduler": "simple",
                "denoise": 1.0,
                "model": ["1", 0],
                "positive": ["4", 0],
                "negative": ["5", 0],
                "latent_image": ["6", 0]
            },
            "class_type": "KSampler"
        },
        "8": {
            "inputs": {
                "samples": ["7", 0],
                "vae": ["3", 0]
            },
            "class_type": "VAEDecode"
        },
        "9": {
            "inputs": {
                "filename_prefix": "Darkness_Sprite",
                "images": ["8", 0]
            },
            "class_type": "SaveImage"
        }
    }
    return workflow

def queue_prompt(server_url: str, workflow: dict) -> str:
    data = json.dumps({"prompt": workflow}).encode('utf-8')
    req = urllib.request.Request(f"{server_url}/prompt", data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req) as resp:
        res = json.loads(resp.read().decode('utf-8'))
        return res['prompt_id']

def wait_for_completion(server_url: str, prompt_id: str, timeout: int = 600) -> dict:
    start_time = time.time()
    while time.time() - start_time < timeout:
        req = urllib.request.Request(f"{server_url}/history/{prompt_id}")
        try:
            with urllib.request.urlopen(req) as resp:
                history = json.loads(resp.read().decode('utf-8'))
                if prompt_id in history:
                    return history[prompt_id]
        except Exception:
            pass
        time.sleep(2)
    raise TimeoutError(f"Generation timed out after {timeout} seconds.")

def download_image(server_url: str, image_info: dict, output_path: str):
    filename = image_info['filename']
    subfolder = image_info.get('subfolder', '')
    img_type = image_info.get('type', 'output')
    
    query = urllib.parse.urlencode({'filename': filename, 'subfolder': subfolder, 'type': img_type})
    url = f"{server_url}/view?{query}"
    
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    urllib.request.urlretrieve(url, output_path)

def main():
    parser = argparse.ArgumentParser(description="Generate Pixel Art Sprites via local ComfyUI API")
    parser.add_argument("--prompt", required=True, help="Description of the sprite to generate")
    parser.add_argument("--output", required=True, help="Destination file path (e.g. Darkness.Godot/assets/sprites/weapon.png)")
    parser.add_argument("--width", type=int, default=512, help="Image width")
    parser.add_argument("--height", type=int, default=512, help="Image height")
    parser.add_argument("--steps", type=int, default=4, help="Inference steps (schnell is a 4-step distilled model)")
    parser.add_argument("--seed", type=int, default=-1, help="Random seed")
    parser.add_argument("--server", default=DEFAULT_SERVER, help="ComfyUI server URL")

    args = parser.parse_args()

    print(f"[*] Queueing sprite generation for: '{args.prompt}'...")
    workflow = build_flux_gguf_workflow(args.prompt, args.width, args.height, args.steps, args.seed)
    
    try:
        prompt_id = queue_prompt(args.server, workflow)
        print(f"[*] Prompt ID: {prompt_id}. Waiting for ComfyUI to finish rendering...")
        
        result = wait_for_completion(args.server, prompt_id)
        
        outputs = result.get('outputs', {})
        for node_id, node_output in outputs.items():
            if 'images' in node_output:
                for img in node_output['images']:
                    download_image(args.server, img, args.output)
                    print(f"[OK] Sprite successfully saved to: {os.path.abspath(args.output)}")
                    return
        print("[!] Error: Generation completed but no output image was returned.")
    except Exception as e:
        print(f"[!] Error during generation: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
