import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import React from "react";

interface ConfirmationDialogProps {
    title: string;
    description: string;
    okName: string;
    cancelName: string;
    open: boolean;
    onCloseDialog(action: boolean): void;
}

const ConfirmationDialog = ({
    title,
    description,
    okName,
    cancelName,
    open,
    onCloseDialog,
}: ConfirmationDialogProps) => {
    return (
        <Dialog open={open} onClose={() => onCloseDialog(false)}>
            <DialogTitle>{title}</DialogTitle>
            <DialogContent>
                <DialogContentText>{description}</DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button onClick={() => onCloseDialog(false)}>{cancelName}</Button>
                <Button variant="contained" onClick={() => onCloseDialog(true)}>
                    {okName}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ConfirmationDialog;
