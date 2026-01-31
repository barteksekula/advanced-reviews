import "./pin.scss";

import Icon from "@mui/material/Icon";
import classNames from "classnames";
import React, { CSSProperties, FunctionComponent, useEffect, useRef } from "react";

import priorityIconMappings from "../store/priority-icon-mappings";
import { Dimensions, PinLocation, Priority } from "../store/review-store";

interface PinProps {
    location: PinLocation;
    highlighted?: boolean;
    showDialog(e: any): void;
    position: Dimensions;
}

const Pin: FunctionComponent<PinProps> = (props: PinProps) => {
    const { highlighted, location, showDialog } = props;

    const div = useRef(null);

    useEffect(() => {
        if (highlighted) {
            div.current.scrollIntoView({
                block: "center",
                inline: "center",
            });
        }
    });

    const circleSize = 12;

    const style: CSSProperties = {
        zIndex: 700,
        left: props.position.x - circleSize + "px",
        top: props.position.y - circleSize + "px",
    };

    return (
        <div
            style={style}
            ref={div}
            onClick={showDialog}
            className={classNames("review-location", {
                done: location.isDone,
                new: location.isUpdatedReview,
                highlighted: highlighted,
            })}
        >
            <svg height="28" width="28">
                <title>{location.formattedFirstComment}</title>
                <circle cx="14" cy="14" r={circleSize} strokeWidth="2" fill="#c0c0c0" />
                <text x="50%" y="50%" dominantBaseline="middle" textAnchor="middle" fontWeight="bold" fill="black" />
            </svg>
            {location.priority !== Priority.Normal && (
                <Icon className="priority-icon" title={location.priority}>
                    {priorityIconMappings[location.priority]}
                </Icon>
            )}
        </div>
    );
};

export default Pin;
