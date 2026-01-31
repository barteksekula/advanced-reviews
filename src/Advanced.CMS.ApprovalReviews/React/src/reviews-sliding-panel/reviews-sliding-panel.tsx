import "./reviews-sliding-panel.scss";

import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import Icon from "@mui/material/Icon";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import Switch from "@mui/material/Switch";
import classNames from "classnames";
import { reaction } from "mobx";
import { inject, observer } from "mobx-react";
import React, { useEffect, useState } from "react";

import Comment from "../comment/comment";
import Confirmation from "../confirmation/confirmation";
import ReviewDetails from "../details/review-details";
import PinNavigator from "../pin-navigator/pin-navigator";
import { IReviewComponentStore, PinLocation, Priority } from "../store/review-store";

interface SlidingPanelProps {
    iframe?: HTMLIFrameElement;
    reviewStore?: IReviewComponentStore;
    resources?: ReviewResources;
}

const Legend = inject("resources")(
    observer(({ resources, filter }) => {
        return (
            <div className="type-filters">
                <div className="filter" title={resources.panel.reviewmode}>
                    <Checkbox checked={filter.reviewMode} onChange={() => (filter.reviewMode = !filter.reviewMode)} />
                </div>
                {filter.reviewMode && (
                    <>
                        <div className="filter unread" title={resources.panel.showunread}>
                            <Checkbox
                                checked={filter.showUnread}
                                onChange={() => (filter.showUnread = !filter.showUnread)}
                            />
                        </div>
                        <div className="filter active" title={resources.panel.showactive}>
                            <Checkbox
                                checked={filter.showActive}
                                onChange={() => (filter.showActive = !filter.showActive)}
                            />
                        </div>
                        <div className="filter resolved" title={resources.panel.showresolved}>
                            <Checkbox
                                checked={filter.showResolved}
                                onChange={() => (filter.showResolved = !filter.showResolved)}
                            />
                        </div>
                    </>
                )}
            </div>
        );
    }),
);

const PinTypeFilters = inject("resources")(
    observer(({ resources, filter }) => {
        return (
            <>
                <h3>Filters</h3>
                <div className="type-filters">
                    <div className="filter unread" title={resources.panel.showunread}>
                        <Switch
                            id="showUnread"
                            checked={filter.showUnread}
                            onChange={() => (filter.showUnread = !filter.showUnread)}
                        />
                        <label htmlFor="showUnread">{resources.panel.showunread}</label>
                    </div>
                    <div className="filter active" title={resources.panel.showactive}>
                        <Switch
                            id="showActive"
                            checked={filter.showActive}
                            onChange={() => (filter.showActive = !filter.showActive)}
                        />
                        <label htmlFor="showActive">{resources.panel.showactive}</label>
                    </div>
                    <div className="filter resolved" title={resources.panel.showresolved}>
                        <Switch
                            id="showResolved"
                            checked={filter.showResolved}
                            onChange={() => (filter.showResolved = !filter.showResolved)}
                        />
                        <label htmlFor="showResolved">{resources.panel.showresolved}</label>
                    </div>
                </div>
            </>
        );
    }),
);

const Filters = inject("resources")(
    observer(({ resources, filter }) => {
        return (
            <div>
                <div className="filter" title={resources.panel.reviewmode}>
                    <Switch
                        id="modeSwitcher"
                        checked={filter.reviewMode}
                        onChange={() => (filter.reviewMode = !filter.reviewMode)}
                    />
                    <label htmlFor="modeSwitcher">{resources.panel.reviewmode}</label>
                </div>
                {filter.reviewMode && <PinTypeFilters filter={filter} />}
            </div>
        );
    }),
);

const SlidingPanel: React.FC<SlidingPanelProps> = ({ iframe, reviewStore, resources }) => {
    const [panelVisible, setPanelVisible] = useState(false);
    const [currentPinToRemove, setCurrentPinToRemove] = useState<PinLocation>(null);

    useEffect(() => {
        const dispose = reaction(
            () => {
                return reviewStore.editedPinLocation;
            },
            () => {
                setPanelVisible(true);
                if (reviewStore.editedPinLocation) {
                    reviewStore.editedPinLocation.updateCurrentUserLastRead();
                }
            },
        );

        return () => {
            dispose();
        };
    }, [reviewStore]);

    const onSelected = (index: number): void => {
        //TODO: implement scroll into view for point
        reviewStore.selectedPinLocation = reviewStore.reviewLocations[index];
    };

    const showPanel = () => {
        reviewStore.selectedPinLocation = null;
        reviewStore.editedPinLocation = null;
        setPanelVisible(true);
    };

    const hidePanel = () => {
        reviewStore.selectedPinLocation = null;
        reviewStore.editedPinLocation = null;
        setPanelVisible(false);
    };

    const onEditClick = (e: any, location: PinLocation) => {
        e.stopPropagation();
        reviewStore.selectedPinLocation = location;
        reviewStore.editedPinLocation = location;
    };

    const resolveTask = () => {
        reviewStore.toggleResolve();
    };

    const onRemove = (action: boolean) => {
        const pinToRemove = currentPinToRemove;
        setCurrentPinToRemove(null);
        if (!action) {
            return;
        }
        reviewStore.remove(pinToRemove);
    };

    const { editedPinLocation, filter, reviewLocations, currentUser } = reviewStore!;
    const res = resources!;

    const chipPropertyNameSettings = {
        title: editedPinLocation && reviewStore.resolvePropertyDisplayName(editedPinLocation.propertyName),
    };

    return (
        <>
            {!panelVisible && (
                <>
                    <div className="panel-container-settings narrow">
                        <IconButton title={res.panel.expand} onClick={showPanel}>
                            <Icon>first_page</Icon>
                        </IconButton>
                    </div>
                    <div className={classNames("panel-container narrow", filter.reviewMode ? "review-mode" : "")}>
                        <Legend filter={filter} />
                    </div>
                </>
            )}
            {panelVisible && (
                <>
                    <div className="panel-container-settings">
                        <IconButton className="close-panel" onClick={hidePanel} title={res.panel.collapse}>
                            <Icon>last_page</Icon>
                        </IconButton>
                    </div>
                    <div className="panel-container">
                        {editedPinLocation && (
                            <div className="panel-header">
                                <Checkbox
                                    id="resolved"
                                    checked={reviewStore.editedPinLocation.isDone}
                                    onChange={resolveTask}
                                />
                                <label htmlFor="resolved">{res.panel.resolved}</label>
                                {editedPinLocation.propertyName && (
                                    <Chip
                                        sx={{
                                            backgroundColor: "#673ab7", // $secondary700 from your SCSS
                                            color: "#fafafa", // $surface50 from your SCSS
                                            marginLeft: "8px",
                                            "& .MuiChip-icon": {
                                                color: "#fafafa", // Match the text color
                                            },
                                            "& .MuiChip-label": {
                                                maxWidth: "100px",
                                                textOverflow: "ellipsis",
                                                overflow: "hidden",
                                            },
                                        }}
                                        label={reviewStore.resolvePropertyDisplayName(editedPinLocation.propertyName)}
                                        icon={<Icon>bookmark</Icon>}
                                        {...chipPropertyNameSettings}
                                    />
                                )}
                                <PinNavigator />
                            </div>
                        )}
                        {!editedPinLocation && (
                            <>
                                <Filters filter={filter} />
                                <h3>List of Pins</h3>
                                <List className="locations">
                                    {reviewLocations.map((location, index) => (
                                        <ListItem
                                            key={location.id}
                                            disablePadding
                                            secondaryAction={
                                                <>
                                                    {location.comments.length === 0 &&
                                                        location.firstComment.author === currentUser && (
                                                            <IconButton
                                                                className="delete"
                                                                title={res.removepindialog.title}
                                                                onClick={() => setCurrentPinToRemove(location)}
                                                                edge="end"
                                                                sx={{ mr: 1 }}
                                                            >
                                                                <Icon>delete</Icon>
                                                            </IconButton>
                                                        )}
                                                    <IconButton
                                                        className="edit"
                                                        title={res.panel.opendetails}
                                                        onClick={(e) => onEditClick(e, location)}
                                                        edge="end"
                                                    >
                                                        <Icon>edit</Icon>
                                                    </IconButton>
                                                </>
                                            }
                                        >
                                            <ListItemButton
                                                title={res.panel.clicktoedit}
                                                selected={reviewStore.selectedPinLocationIndex === index}
                                                onClick={() => onSelected(index)}
                                                onDoubleClick={(e) => onEditClick(e, location)}
                                            >
                                                <Comment
                                                    comment={location.firstComment}
                                                    isImportant={location.priority === Priority.Important}
                                                    isDone={location.isDone}
                                                />
                                            </ListItemButton>
                                        </ListItem>
                                    ))}
                                </List>
                            </>
                        )}
                        {editedPinLocation && (
                            <ReviewDetails
                                onCancel={() => (reviewStore.editedPinLocation = null)}
                                iframe={iframe}
                                currentEditLocation={reviewStore.editedPinLocation}
                            />
                        )}
                        {!!currentPinToRemove && (
                            <Confirmation
                                title={res.removepindialog.title}
                                description={res.removepindialog.description}
                                okName={res.removepindialog.ok}
                                cancelName={res.removepindialog.cancel}
                                open={!!currentPinToRemove}
                                onCloseDialog={onRemove}
                            />
                        )}
                    </div>
                </>
            )}
        </>
    );
};

export default inject("resources", "reviewStore")(observer(SlidingPanel));
