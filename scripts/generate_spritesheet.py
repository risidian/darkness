#!/usr/bin/env python3
"""
ComfyUI Automated Pixel Art / Sprite Sheet Generator for Darkness Godot Game
Connects to ComfyUI REST API and generates LPC-compatible sheets.
"""

import sys
import os
import json
import time
import random
import argparse
import urllib.request
import urllib.parse
try:
    from PIL import Image
except ImportError:
    print("[!] Error: Pillow (PIL) is not installed. Please run 'pip install Pillow'")
    sys.exit(1)

DEFAULT_SERVER = "http://127.0.0.1:8188"

# LPC Constants
FRAME_SIZE = 64
SHEET_WIDTH = 1536
SHEET_HEIGHT = 3456
COLS = 24
ROWS = 54

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

def download_image(server_url: str, image_info: dict) -> Image.Image:
    filename = image_info['filename']
    subfolder = image_info.get('subfolder', '')
    img_type = image_info.get('type', 'output')
    
    query = urllib.parse.urlencode({'filename': filename, 'subfolder': subfolder, 'type': img_type})
    url = f"{server_url}/view?{query}"
    
    req = urllib.request.urlopen(url)
    img = Image.open(req).convert("RGBA")
    return img

def main():
    parser = argparse.ArgumentParser(description="Generate Pixel Art Sprite Sheet via local ComfyUI API")
    parser.add_argument("--prompt", required=True, help="Description of the sprite to generate")
    parser.add_argument("--output", required=True, help="Destination file path")
    parser.add_argument("--width", type=int, default=512, help="Image width")
    parser.add_argument("--height", type=int, default=512, help="Image height")
    parser.add_argument("--steps", type=int, default=4, help="Inference steps")
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
                for img_info in node_output['images']:
                    base_img = download_image(args.server, img_info)
                    
                    # Process into 64x64
                    base_img.thumbnail((FRAME_SIZE, FRAME_SIZE), Image.Resampling.LANCZOS)
                    
                    # Create blank 64x64 for pasting if thumbnail ratio isn't 1:1
                    frame = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
                    offset_x = (FRAME_SIZE - base_img.width) // 2
                    offset_y = (FRAME_SIZE - base_img.height) // 2
                    frame.paste(base_img, (offset_x, offset_y))
                    
                    # Create LPC sheet
                    print(f"[*] Building {SHEET_WIDTH}x{SHEET_HEIGHT} sprite sheet...")
                    sheet = Image.new("RGBA", (SHEET_WIDTH, SHEET_HEIGHT), (0, 0, 0, 0))
                    for row in range(ROWS):
                        for col in range(COLS):
                            sheet.paste(frame, (col * FRAME_SIZE, row * FRAME_SIZE))
                            
                    os.makedirs(os.path.dirname(os.path.abspath(args.output)), exist_ok=True)
                    sheet.save(args.output)
                    print(f"[OK] Sprite sheet successfully saved to: {os.path.abspath(args.output)}")
                    return
        print("[!] Error: Generation completed but no output image was returned.")
    except Exception as e:
        print(f"[!] Error during generation: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
