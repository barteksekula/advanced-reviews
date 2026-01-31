import Button from "@mui/material/Button";
import { inject } from "mobx-react";
import React from "react";

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

class DrawablePreview extends React.Component<DrawablePreviewProps, any> {
    canvasRef: React.RefObject<HTMLCanvasElement>;

    constructor(props: any) {
        super(props);
        this.canvasRef = React.createRef<HTMLCanvasElement>();
        this.state = {
            isDown: false,
            previousPointX: 0,
            previousPointY: 0,
        };
    }

    handleMouseDown = (event) => {
        this.setState({
            isDown: true,
            previousPointX: event.offsetX,
            previousPointY: event.offsetY,
        });

        const ctx = this.canvasRef.current.getContext("2d");
        ctx.moveTo(event.offsetX, event.offsetY);
    };
    handleMouseMove = (event) => {
        if (!this.state.isDown) {
            return;
        }

        const ctx = this.canvasRef.current.getContext("2d");
        ctx.moveTo(this.state.previousPointX, this.state.previousPointY);
        ctx.lineTo(event.offsetX, event.offsetY);
        ctx.strokeStyle = "red";
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.closePath();

        this.setState({
            previousPointX: event.offsetX,
            previousPointY: event.offsetY,
        });
    };
    handleMouseUp = () => {
        this.setState({
            isDown: false,
        });
    };

    componentDidMount(): void {
        drawImageOnCanvas(this.props.src, this.canvasRef.current);
    }

    cancel = () => {
        this.clear();
        this.props.onCancel();
    };

    clear = () => {
        drawImageOnCanvas(this.props.src, this.canvasRef.current);
    };

    done = () => {
        this.props.onApplyDrawing(this.canvasRef.current.toDataURL());
    };

    render() {
        const { height, resources, width } = this.props;

        const canvasStyle = {
            cursor: "crosshair",
        };

        return (
            <>
                <canvas
                    ref={this.canvasRef}
                    style={canvasStyle}
                    width={width}
                    height={height}
                    onMouseDown={(e) => {
                        this.handleMouseDown(e.nativeEvent);
                    }}
                    onMouseMove={(e) => {
                        this.handleMouseMove(e.nativeEvent);
                    }}
                    onMouseUp={this.handleMouseUp}
                />
                <div className="mdc-dialog__actions">
                    <Button onClick={this.cancel}>{resources.screenshot.cancel}</Button>
                    <Button onClick={this.clear}>{resources.screenshot.clear}</Button>
                    <Button onClick={this.done}>{resources.screenshot.apply}</Button>
                </div>
            </>
        );
    }
}

export default inject("resources")(DrawablePreview);
