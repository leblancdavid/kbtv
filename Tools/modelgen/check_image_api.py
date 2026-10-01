"""Verify OPENAI_API_KEY without spending image-generation credits.

Run: python Tools/modelgen/check_image_api.py
The key is read from the environment only; it is never printed or saved.
"""

import json
import os
import sys
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def main():
    key = os.environ.get("OPENAI_API_KEY", "").strip()
    if not key:
        print("OPENAI_API_KEY is not available in this terminal.")
        print("Set it as a user environment variable, reopen the terminal, and retry.")
        return 1

    request = Request(
        "https://api.openai.com/v1/models",
        headers={"Authorization": f"Bearer {key}"},
    )
    try:
        with urlopen(request, timeout=20) as response:
            models = json.load(response)
    except HTTPError as error:
        print(f"Image API credential check returned HTTP {error.code}.")
        if error.code == 401:
            print("The API rejected this key. Check that it is an OpenAI Platform API key.")
        return 1
    except (URLError, TimeoutError, ValueError) as error:
        print(f"Could not complete the OpenAI API connection check: {type(error).__name__}")
        return 1

    names = {model.get("id") for model in models.get("data", [])}
    print("OpenAI API key accepted. No image was generated or billed.")
    model = "gpt-image-2.5-flare"
    print(f"{model} listed for this account: {'yes' if model in names else 'not listed'}")
    print("A listed model does not confirm image-generation billing or verification.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
