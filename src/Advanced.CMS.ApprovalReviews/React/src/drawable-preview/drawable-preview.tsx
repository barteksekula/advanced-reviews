import Button from "@mui/material/Button";
import { inject } from "mobx-react";
import React, { useEffect, useRef, useState } from "react";

interface DrawablePreviewProps {
    width: number;
    height: number;
    src?: string;
    onApplyDrawing: (string) => void;
    onCancel: () => void;
    resources?: ReviewResources;
}

function drawImageOnCanvas(base64Image, canvas) {
    if (!base64Image) {
        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        return;
    }

    const img = new Image();
    img.src = base64Image;
    img.onload = function () {
        const ctx = canvas.getContext("2d");
        ctx.beginPath();
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(img, 0, 0, img.width, img.height);
    };
}

const DrawablePreview: React.FC<DrawablePreviewProps> = ({
    width,
    height,
    src,
    onApplyDrawing,
    onCancel,
    resources,
}) => {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const [isDown, setIsDown] = useState(false);
    const [previousPointX, setPreviousPointX] = useState(0);
    const [previousPointY, setPreviousPointY] = useState(0);

    useEffect(() => {
        drawImageOnCanvas(src, canvasRef.current);
    }, [src]);

    const handleMouseDown = (event) => {
        setIsDown(true);
        setPreviousPointX(event.offsetX);
        setPreviousPointY(event.offsetY);

        const ctx = canvasRef.current.getContext("2d");
        ctx.moveTo(event.offsetX, event.offsetY);
    };

    const handleMouseMove = (event) => {
        if (!isDown) {
            return;
        }

        const ctx = canvasRef.current.getContext("2d");
        ctx.moveTo(previousPointX, previousPointY);
        ctx.lineTo(event.offsetX, event.offsetY);
        ctx.strokeStyle = "red";
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.closePath();

        setPreviousPointX(event.offsetX);
        setPreviousPointY(event.offsetY);
    };

    const handleMouseUp = () => {
        setIsDown(false);
    };

    const cancel = () => {
        clear();
        onCancel();
    };

    const clear = () => {
        drawImageOnCanvas(src, canvasRef.current);
    };

    const done = () => {
        onApplyDrawing(canvasRef.current.toDataURL());
    };

    const canvasStyle = {
        cursor: "crosshair",
    };

    return (
        <>
            <canvas
                ref={canvasRef}
                style={canvasStyle}
                width={width}
                height={height}
                onMouseDown={(e) => {
                    handleMouseDown(e.nativeEvent);
                }}
                onMouseMove={(e) => {
                    handleMouseMove(e.nativeEvent);
                }}
                onMouseUp={handleMouseUp}
            />
            <div className="mdc-dialog__actions">
                <Button onClick={cancel}>{resources.screenshot.cancel}</Button>
                <Button onClick={clear}>{resources.screenshot.clear}</Button>
                <Button onClick={done}>{resources.screenshot.apply}</Button>
            </div>
        </>
    );
};

export default inject("resources")(DrawablePreview);
