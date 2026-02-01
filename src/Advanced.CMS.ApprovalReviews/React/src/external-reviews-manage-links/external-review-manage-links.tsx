import "./external-review-manage-links.scss";

import Icon from "@mui/material/Icon";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import { format } from "date-fns";
import { observer } from "mobx-react-lite";
import React, { useState } from "react";

import Confirmation from "../confirmation/confirmation";
import { IExternalReviewStore, ReviewLink } from "./external-review-links-store";
import LinkEditDialog from "./external-review-manage-links-edit";
import ShareDialog, { LinkShareResult } from "./external-review-share-dialog";

export interface VisitorGroup {
    id: string;
    name: string;
}

export interface ExternalReviewWidgetContentProps {
    store: IExternalReviewStore;
    resources: ExternalReviewResources;
    availableVisitorGroups: VisitorGroup[];
    editableLinksEnabled: boolean;
    pinCodeSecurityEnabled: boolean;
    pinCodeSecurityRequired?: boolean;
    pinCodeLength: number;
    prolongDays: number;
}

/**
 * Component used to render list of external review links
 */
const ExternalReviewWidgetContent = observer(
    ({
        store,
        resources,
        availableVisitorGroups,
        editableLinksEnabled,
        pinCodeSecurityEnabled,
        pinCodeSecurityRequired,
        pinCodeLength,
        prolongDays,
    }: ExternalReviewWidgetContentProps) => {
        const [currentLinkToDelete, setLinkToDelete] = useState<ReviewLink>(null);
        const [currentLinkToShare, setLinkToShare] = useState<ReviewLink>(null);
        const [currentLinkToEdit, setLinkToEdit] = useState<ReviewLink>(null);
        const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

        const isPinRequired = pinCodeSecurityEnabled && pinCodeSecurityRequired;

        const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
            setAnchorEl(event.currentTarget);
        };

        const handleMenuClose = () => {
            setAnchorEl(null);
        };

        const onDelete = (action: boolean) => {
            setLinkToDelete(null);
            if (!action) {
                return;
            }
            store.delete(currentLinkToDelete);
        };

        const onShareDialogClose = (shareLink: LinkShareResult) => {
            setLinkToShare(null);
            if (shareLink === null) {
                return;
            }
            store.share(currentLinkToShare, shareLink.email, shareLink.subject, shareLink.message);
        };

        const onEditClose = async (validTo: Date, pinCode: string, displayName: string, visitorGroups: string[]) => {
            setLinkToEdit(null);
            if (!validTo) {
                return;
            }
            if (currentLinkToEdit.isPersisted) {
                store.edit(currentLinkToEdit, validTo, pinCode, displayName, visitorGroups);
                return;
            }

            if (isPinRequired && !pinCode) {
                return;
            }

            const reviewLink = await store.addLink(currentLinkToEdit.isEditable);
            store.edit(reviewLink, null, pinCode, displayName, visitorGroups);
        };

        const addNewLink = (isEditable) => {
            handleMenuClose();
            if (isEditable || !isPinRequired) {
                store.addLink(isEditable);
                return;
            }

            const temporaryLink = new ReviewLink(null, null, null, null, isEditable);
            setLinkToEdit(temporaryLink);
        };

        if (!store.enabled) {
            return (
                <div className="empty-list">
                    <span>{resources.list.onlypages}</span>
                </div>
            );
        }

        return (
            <>
                {store.links.length === 0 && (
                    <div className="empty-list">
                        <span>{resources.list.emptylist}</span>
                    </div>
                )}

                {store.links.length > 0 && (
                    <List className="external-reviews-list">
                        {store.links.map((item: ReviewLink) => {
                            const link = item.isActive ? (
                                <a href={item.linkUrl} target="_blank" rel="noopener noreferrer">
                                    {item.displayName || item.token}
                                </a>
                            ) : (
                                <span className="item-inactive">{item.token}</span>
                            );

                            const icon = <Icon>{item.isEditable ? "rate_review" : "pageview"}</Icon>;

                            return (
                                <ListItem
                                    key={item.token}
                                    className="list-item"
                                    secondaryAction={
                                        <>
                                            <IconButton
                                                className="item-action"
                                                title={resources.list.editlink}
                                                onClick={() => setLinkToEdit(item)}
                                                edge="end"
                                                sx={{ mr: 1 }}
                                            >
                                                <Icon>edit</Icon>
                                            </IconButton>
                                            <IconButton
                                                className="item-action"
                                                disabled={!item.isActive}
                                                title={resources.list.sharetitle}
                                                onClick={() => setLinkToShare(item)}
                                                edge="end"
                                                sx={{ mr: 1 }}
                                            >
                                                <Icon>share</Icon>
                                            </IconButton>
                                            <IconButton
                                                className="item-action"
                                                title={resources.list.deletetitle}
                                                onClick={() => setLinkToDelete(item)}
                                                edge="end"
                                            >
                                                <Icon>delete_outline</Icon>
                                            </IconButton>
                                        </>
                                    }
                                >
                                    {editableLinksEnabled && <ListItemIcon>{icon}</ListItemIcon>}
                                    <ListItemText
                                        primary={link}
                                        secondary={
                                            resources.list.itemvalidto +
                                            ": " +
                                            format(item.validTo, "MMM do yyyy HH:mm")
                                        }
                                    />
                                    <div className="info-icons">
                                        {item.pinCode && pinCodeSecurityEnabled && (
                                            <Icon
                                                className="link-secured"
                                                title={resources.list.editdialog.linksecured}
                                            >
                                                lock
                                            </Icon>
                                        )}
                                        {item.visitorGroups && item.visitorGroups.length > 0 && (
                                            <Icon className="link-secured" title="Visitor groups applied">
                                                groups
                                            </Icon>
                                        )}
                                        {item.projectId > 0 && (
                                            <span
                                                className="dijitReset dijitInline dijitIcon epi-iconProject"
                                                title={resources.list.projectname + ": " + item.projectName}
                                            ></span>
                                        )}
                                    </div>
                                </ListItem>
                            );
                        })}
                    </List>
                )}
                <div>
                    {editableLinksEnabled ? (
                        <>
                            <IconButton title="Add link" onClick={handleMenuOpen}>
                                <Icon>playlist_add</Icon>
                            </IconButton>
                            <Menu
                                anchorEl={anchorEl}
                                open={Boolean(anchorEl)}
                                onClose={handleMenuClose}
                                anchorOrigin={{
                                    vertical: "bottom",
                                    horizontal: "left",
                                }}
                            >
                                <MenuItem onClick={() => addNewLink(false)}>
                                    <ListItemIcon>
                                        <Icon>pageview</Icon>
                                    </ListItemIcon>
                                    <ListItemText>{resources.list.viewlink}</ListItemText>
                                </MenuItem>
                                <MenuItem onClick={() => addNewLink(true)}>
                                    <ListItemIcon>
                                        <Icon>rate_review</Icon>
                                    </ListItemIcon>
                                    <ListItemText>{resources.list.editlink}</ListItemText>
                                </MenuItem>
                            </Menu>
                        </>
                    ) : (
                        <IconButton title="Add link" onClick={() => addNewLink(false)}>
                            <Icon>playlist_add</Icon>
                        </IconButton>
                    )}
                </div>
                {!!currentLinkToDelete && (
                    <Confirmation
                        title={resources.removedialog.title}
                        description={resources.removedialog.description}
                        okName={resources.removedialog.ok}
                        cancelName={resources.removedialog.cancel}
                        open={!!currentLinkToDelete}
                        onCloseDialog={onDelete}
                    />
                )}

                {!!currentLinkToShare && (
                    <ShareDialog
                        open={!!currentLinkToShare}
                        onClose={onShareDialogClose}
                        initialSubject={store.initialMailSubject}
                        initialMessage={
                            currentLinkToShare && currentLinkToShare.isEditable
                                ? store.initialEditMailMessage
                                : store.initialViewMailMessage
                        }
                        resources={resources}
                    />
                )}
                {!!currentLinkToEdit && (
                    <LinkEditDialog
                        reviewLink={currentLinkToEdit}
                        onClose={onEditClose}
                        resources={resources}
                        availableVisitorGroups={availableVisitorGroups}
                        open={!!currentLinkToEdit}
                        pinCodeSecurityEnabled={pinCodeSecurityEnabled}
                        pinCodeSecurityRequired={pinCodeSecurityRequired}
                        pinCodeLength={pinCodeLength}
                        prolongDays={prolongDays}
                    />
                )}
            </>
        );
    },
);

export default ExternalReviewWidgetContent;
