import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import TextField from "@mui/material/TextField";
import React, { useState } from "react";

interface ConfirmDialogProps {
    open: boolean;
    onClose(userName: string): void;
    initialUserName?: string;
}

const ConfirmDialog = ({ open, onClose, initialUserName }: ConfirmDialogProps) => {
    const [userName, setUserName] = useState<string>(initialUserName);

    const onDialogClose = () => {
        onClose(null);
    };

    const onSave = () => {
        onClose(userName);
    };

    return (
        <Dialog open={open} onClose={onDialogClose}>
            <DialogTitle>Confirm your name</DialogTitle>
            <DialogContent>
                <p>Please enter your name. It will be used as an author of the comments.</p>
                <TextField
                    label="Display name"
                    autoFocus
                    required
                    fullWidth
                    value={userName}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setUserName(e.target.value)}
                    error={!userName}
                    margin="normal"
                />
            </DialogContent>
            <DialogActions>
                <Button variant="contained" onClick={onSave} disabled={!userName}>
                    Save
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ConfirmDialog;
