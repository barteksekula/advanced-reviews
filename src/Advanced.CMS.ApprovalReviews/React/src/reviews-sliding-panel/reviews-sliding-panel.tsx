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
import { IReactionDisposer, reaction } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";

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

class SlidingPanel extends React.Component<SlidingPanelProps, any> {
    locationChangedReaction: IReactionDisposer;

    constructor(props: SlidingPanelProps) {
        super(props);
        this.state = {
            panelVisible: false,
            currentPinToRemove: null,
        };

        this.locationChangedReaction = reaction(
            () => {
                return this.props.reviewStore.editedPinLocation;
            },
            () => {
                this.setState({ panelVisible: true });
                if (this.props.reviewStore.editedPinLocation) {
                    this.props.reviewStore.editedPinLocation.updateCurrentUserLastRead();
                }
            },
        );
    }

    onSelected = (index: number): void => {
        //TODO: implement scroll into view for point
        this.props.reviewStore.selectedPinLocation = this.props.reviewStore.reviewLocations[index];
    };

    showPanel = () => {
        this.props.reviewStore.selectedPinLocation = null;
        this.props.reviewStore.editedPinLocation = null;
        this.setState({ panelVisible: true });
    };

    hidePanel = () => {
        this.props.reviewStore.selectedPinLocation = null;
        this.props.reviewStore.editedPinLocation = null;
        this.setState({ panelVisible: false });
    };

    onEditClick(e: any, location: PinLocation) {
        e.stopPropagation();
        this.props.reviewStore.selectedPinLocation = location;
        this.props.reviewStore.editedPinLocation = location;
    }

    resolveTask = () => {
        this.props.reviewStore.toggleResolve();
    };

    onRemove = (action: boolean) => {
        const pinToRemove = this.state.currentPinToRemove;
        this.setState({ currentPinToRemove: null });
        if (!action) {
            return;
        }
        this.props.reviewStore.remove(pinToRemove);
    };

    render() {
        const { editedPinLocation, filter, reviewLocations, currentUser } = this.props.reviewStore!;
        const res = this.props.resources!;

        const chipPropertyNameSettings = {
            title:
                editedPinLocation && this.props.reviewStore.resolvePropertyDisplayName(editedPinLocation.propertyName),
        };

        return (
            <>
                {!this.state.panelVisible && (
                    <>
                        <div className="panel-container-settings narrow">
                            <IconButton title={res.panel.expand} onClick={this.showPanel}>
                                <Icon>first_page</Icon>
                            </IconButton>
                        </div>
                        <div className={classNames("panel-container narrow", filter.reviewMode ? "review-mode" : "")}>
                            <Legend filter={filter} />
                        </div>
                    </>
                )}
                {this.state.panelVisible && (
                    <>
                        <div className="panel-container-settings">
                            <IconButton className="close-panel" onClick={this.hidePanel} title={res.panel.collapse}>
                                <Icon>last_page</Icon>
                            </IconButton>
                        </div>
                        <div className="panel-container">
                            {editedPinLocation && (
                                <div className="panel-header">
                                    <Checkbox
                                        id="resolved"
                                        checked={this.props.reviewStore.editedPinLocation.isDone}
                                        onChange={this.resolveTask}
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
                                            label={this.props.reviewStore.resolvePropertyDisplayName(
                                                editedPinLocation.propertyName,
                                            )}
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
                                                                    onClick={() =>
                                                                        this.setState({ currentPinToRemove: location })
                                                                    }
                                                                    edge="end"
                                                                    sx={{ mr: 1 }}
                                                                >
                                                                    <Icon>delete</Icon>
                                                                </IconButton>
                                                            )}
                                                        <IconButton
                                                            className="edit"
                                                            title={res.panel.opendetails}
                                                            onClick={(e) => this.onEditClick(e, location)}
                                                            edge="end"
                                                        >
                                                            <Icon>edit</Icon>
                                                        </IconButton>
                                                    </>
                                                }
                                            >
                                                <ListItemButton
                                                    title={res.panel.clicktoedit}
                                                    selected={this.props.reviewStore.selectedPinLocationIndex === index}
                                                    onClick={() => this.onSelected(index)}
                                                    onDoubleClick={(e) => this.onEditClick(e, location)}
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
                                    onCancel={() => (this.props.reviewStore.editedPinLocation = null)}
                                    iframe={this.props.iframe}
                                    currentEditLocation={this.props.reviewStore.editedPinLocation}
                                />
                            )}
                            {!!this.state.currentPinToRemove && (
                                <Confirmation
                                    title={res.removepindialog.title}
                                    description={res.removepindialog.description}
                                    okName={res.removepindialog.ok}
                                    cancelName={res.removepindialog.cancel}
                                    open={!!this.state.currentPinToRemove}
                                    onCloseDialog={this.onRemove}
                                />
                            )}
                        </div>
                    </>
                )}
            </>
        );
    }
}

export default inject("resources", "reviewStore")(observer(SlidingPanel));
