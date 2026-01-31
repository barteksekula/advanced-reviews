import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import FormHelperText from "@mui/material/FormHelperText";
import TextField from "@mui/material/TextField";
import { format, parse } from "date-fns";
import { observer } from "mobx-react-lite";
import React, { useState } from "react";

import { ReviewLink } from "./external-review-links-store";
import { VisitorGroup } from "./external-review-manage-links";

interface LinkEditDialogProps {
    reviewLink: ReviewLink;
    onClose(validTo: Date, pinCode: string, displayName: string, visitorGroups: string[]): void;
    resources: ExternalReviewResources;
    availableVisitorGroups: VisitorGroup[];
    open: boolean;
    pinCodeSecurityEnabled: boolean;
    pinCodeSecurityRequired: boolean;
    pinCodeLength: number;
    prolongDays: number;
}

const dateTimeFormat = "yyyy-MM-dd hh:mm";

/**
 * Dialog component used to edit link created in manage links
 */
const LinkEditDialog = observer(
    ({
        reviewLink,
        onClose,
        open,
        resources,
        availableVisitorGroups,
        pinCodeSecurityEnabled,
        pinCodeSecurityRequired,
        pinCodeLength,
        prolongDays,
    }: LinkEditDialogProps) => {
        const [displayName, setDisplayName] = useState<string>(reviewLink.displayName || "");
        const [visitorGroups, setVisitorGroups] = useState<string[]>(reviewLink.visitorGroups || []);
        const [validDate, setValidDate] = useState<string>(format(reviewLink.validTo, dateTimeFormat));
        const [prolongVisible, setProlongVisible] = useState<boolean>(true);
        const [pinCode, setPinCode] = useState<string>(reviewLink.pinCode || "");
        const [shouldUpdatePinCode, setShouldUpdatePinCode] = useState<boolean>(!reviewLink.pinCode);
        // canSave logic:
        // - Editable links don't need PIN, so always allow save
        // - Non-editable links without existing PIN and with PIN required: must enter valid PIN first
        // - All other cases: allow save
        const [canSave, setCanSave] = useState(() => {
            if (reviewLink.isEditable) {
                return true; // Editable links don't use PIN security
            }
            if (!reviewLink.pinCode && pinCodeSecurityRequired) {
                return false; // New non-editable link with PIN required - must enter valid PIN
            }
            return true; // Existing link or PIN not required
        });

        if (prolongDays <= 0) {
            prolongDays = 5;
        }

        const onCloseDialog = (action: string) => {
            if (validDate && action !== "save") {
                onClose(null, null, null, null);
                return;
            }
            const newPin = shouldUpdatePinCode ? pinCode : null;
            const validTo = parse(validDate, dateTimeFormat, validDate);
            onClose(validTo, newPin, displayName, visitorGroups);
        };

        const updatePinCode = (event: React.ChangeEvent<HTMLInputElement>) => {
            const newValue = event.target.value;
            if (!!newValue && !/^\d+$/.test(newValue)) {
                return;
            }
            setPinCode(newValue);
            const doesPinMatchRequirements = newValue.length === pinCodeLength;
            setCanSave(
                pinCodeSecurityRequired ? doesPinMatchRequirements : newValue.length === 0 || doesPinMatchRequirements,
            );
        };

        const updateValidDate = () => {
            setProlongVisible(false);
            let dateCopy = new Date(reviewLink.validTo.getTime());
            const today = new Date();
            if (today > dateCopy) {
                dateCopy = today;
            }
            dateCopy.setDate(dateCopy.getDate() + prolongDays);

            setValidDate(format(dateCopy, dateTimeFormat));
        };

        const prolongTitle = (resources.list.editdialog.prolongbydays || "").replace(
            "[#days#]",
            prolongDays.toString(),
        );

        return (
            <Dialog open={open} onClose={() => onCloseDialog("cancel")} maxWidth="sm" fullWidth>
                <DialogTitle>
                    {reviewLink.isPersisted ? resources.list.editdialog.title : resources.list.editdialog.newitemtitle}
                </DialogTitle>
                <DialogContent className="external-link-edit-dialog">
                    {reviewLink.isPersisted && (
                        <div className="field-group prolong">
                            <span>
                                {resources.list.editdialog.validto}: {validDate}
                            </span>{" "}
                            {prolongVisible && (
                                <Button variant="text" title={prolongTitle} onClick={updateValidDate}>
                                    Prolong
                                </Button>
                            )}
                        </div>
                    )}
                    {pinCodeSecurityEnabled && !reviewLink.isEditable && (
                        <div className="field-group">
                            {!!reviewLink.pinCode && (
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={shouldUpdatePinCode}
                                            onChange={() => setShouldUpdatePinCode(!shouldUpdatePinCode)}
                                        />
                                    }
                                    label={resources.list.editdialog.pincheckboxlabel}
                                />
                            )}
                            <TextField
                                label={`${resources.list.editdialog.pincode} (${pinCodeLength} ${resources.list.editdialog.digits})`}
                                fullWidth
                                disabled={!shouldUpdatePinCode}
                                value={pinCode}
                                onChange={updatePinCode}
                                required={pinCodeSecurityRequired}
                                type="password"
                                autoComplete="new-password"
                                inputProps={{ maxLength: pinCodeLength }}
                                margin="normal"
                            />
                            <FormHelperText>
                                {!!pinCode
                                    ? resources.list.editdialog.linksecured
                                    : resources.list.editdialog.linknotsecured}
                            </FormHelperText>
                        </div>
                    )}
                    <div className="field-group">
                        <TextField
                            label={resources.list.editdialog.displayname}
                            fullWidth
                            autoFocus
                            value={displayName}
                            onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
                                setDisplayName(event.target.value)
                            }
                            margin="normal"
                        />
                        <FormHelperText>{resources.list.editdialog.displaynamehelptext}</FormHelperText>
                    </div>
                    <div className="field-group">
                        <span>
                            {resources.list.editdialog.visitorgroups} ({visitorGroups.length})
                        </span>
                        <div className="visitor-groups-list">
                            {availableVisitorGroups.map((v) => (
                                <FormControlLabel
                                    key={v.id}
                                    control={
                                        <Checkbox
                                            id={v.id}
                                            checked={visitorGroups.indexOf(v.id) !== -1}
                                            onChange={(e) => {
                                                const selectedGroups = [...visitorGroups];
                                                if (e.target.checked) {
                                                    setVisitorGroups([...selectedGroups, v.id]);
                                                } else {
                                                    selectedGroups.splice(selectedGroups.indexOf(v.id), 1);
                                                    setVisitorGroups([...selectedGroups]);
                                                }
                                            }}
                                        />
                                    }
                                    label={v.name}
                                />
                            ))}
                        </div>
                        <FormHelperText>{resources.list.editdialog.visitorgroupshelptext}</FormHelperText>
                    </div>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => onCloseDialog("cancel")}>{resources.shared.cancel}</Button>
                    <Button disabled={!canSave} onClick={() => onCloseDialog("save")} variant="contained" autoFocus>
                        {resources.shared.save}
                    </Button>
                </DialogActions>
            </Dialog>
        );
    },
);

export default LinkEditDialog;
