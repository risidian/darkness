"""Connect to ComfyUI WebSocket and display real-time progress."""

import asyncio
import json
import sys
import uuid

import websockets


async def monitor_progress(client_id: str | None = None):
    client_id = client_id or str(uuid.uuid4())
    uri = f"ws://127.0.0.1:8188/ws?clientId={client_id}"

    print(f"Connecting to {uri}...")
    async with websockets.connect(uri) as ws:
        print("Connected. Listening for progress...\n")

        async for raw in ws:
            msg = json.loads(raw)
            msg_type = msg.get("type")
            data = msg.get("data", {})

            if msg_type == "progress":
                value = data.get("value", 0)
                max_val = data.get("max", 1)
                prompt_id = data.get("prompt_id", "?")[:8]
                pct = int(value / max_val * 100) if max_val else 0
                bar = "█" * (pct // 5) + "░" * (20 - pct // 5)
                print(f"[{prompt_id}] {bar} {pct}%  (step {value}/{max_val})", end="\r")

            elif msg_type == "executing":
                node = data.get("node")
                prompt_id = data.get("prompt_id", "?")[:8]
                if node:
                    print(f"[{prompt_id}] Executing node {node}...", end="\r")
                else:
                    print(f"\n[{prompt_id}] Done!                            ")

            elif msg_type == "executed":
                prompt_id = data.get("prompt_id", "?")[:8]
                output = data.get("output", {})
                images = output.get("images", [])
                print(f"\n[{prompt_id}] Completed — {len(images)} image(s) generated")

            elif msg_type == "execution_error":
                print(f"\nError: {data.get('exception_message', 'unknown')}")
                return

            elif msg_type == "status":
                queue = data.get("queue_running", [])
                pending = data.get("queue_pending", [])
                if queue:
                    print(f"Queue running: {len(queue)}, pending: {len(pending)}")


if __name__ == "__main__":
    cid = sys.argv[1] if len(sys.argv) > 1 else None
    try:
        asyncio.run(monitor_progress(cid))
    except KeyboardInterrupt:
        print("\nStopped.")
