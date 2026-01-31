import "./pin-collection.scss";

import { inject, observer } from "mobx-react";
import React from "react";

import Pin from "../pin/pin";
import PositionCalculator from "../position-calculator/position-calculator";
import { IReviewComponentStore, PinLocation } from "../store/review-store";

interface PinCollectionProps {
    reviewStore?: IReviewComponentStore;
    newLocation?: PinLocation;
    positionCalculator?: PositionCalculator;
}

class PinCollection extends React.Component<PinCollectionProps> {
    onLocationClick = (e, location: PinLocation) => {
        e.stopPropagation();
        this.props.reviewStore.selectedPinLocation = this.props.reviewStore.editedPinLocation = location;
    };

    render() {
        const { selectedPinLocation, filteredReviewLocations } = this.props.reviewStore!;
        const locations = [...filteredReviewLocations];

        if (this.props.newLocation && !locations.some((location) => location === this.props.newLocation)) {
            locations.push(this.props.newLocation);
        }

        return (
            <div>
                {locations.map((location) => (
                    <Pin
                        key={location.id || "unsaved"}
                        location={location}
                        position={this.props.positionCalculator.calculate(location)}
                        showDialog={(e) => this.onLocationClick(e, location)}
                        highlighted={location === selectedPinLocation}
                    />
                ))}
            </div>
        );
    }
}

export default inject("reviewStore")(observer(PinCollection));
