import io
import soundfile as sf
import librosa
import torch
import numpy as np

from transformers import AutoProcessor, AutoModelForCTC

MODEL_NAME = "nguyenvulebinh/wav2vec2-base-vietnamese-250h"

def load_model():
    MODEL_NAME = "nguyenvulebinh/wav2vec2-base-vietnamese-250h"
    model = AutoModelForCTC.from_pretrained(MODEL_NAME)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model.to(device)
    model.eval()
    return model

def get_device():
    return torch.device("cuda" if torch.cuda.is_available() else "cpu")

def load_processor():
    processor = AutoProcessor.from_pretrained(MODEL_NAME)
    return processor

def audio_transcribe(wavPath, model, processor, device):
    try:
        # Read audio from bytes
        audio_input, sample_rate = sf.read(wavPath)

        # Ensure that the audio has the correct sample rate
        if sample_rate != 16000:
            audio_input = librosa.resample(audio_input, orig_sr=sample_rate, target_sr=16000)

        # Preprocess input data
        input_values = processor(audio_input, return_tensors="pt", padding="longest").input_values
        input_values = input_values.to(device)

        # Predict with the model
        with torch.no_grad():
            logits = model(input_values).logits

        predicted_ids = torch.argmax(logits, dim=-1)

        # Decode to text
        transcription = processor.decode(predicted_ids[0])

        return transcription
    
    except ValueError as ve:
        print(f"ValueError: {ve} - Ensure the audio bytes are valid and compatible.")
    except RuntimeError as re:
        print(f"RuntimeError: {re} - Check the model and processor compatibility with the input.")
    except Exception as e:
        print(f"An error occurred during audio transcription: {e} - Audio input type: {type(audio_input)}")
