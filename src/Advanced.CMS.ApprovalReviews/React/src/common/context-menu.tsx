import Icon from "@mui/material/Icon";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import React, { useState } from "react";

import { DropDownMenu } from "./drop-down-menu";

interface MenuItem {
    name: string;
    icon?: string;
    onSelected: () => void;
}

interface ContextMenuProps {
    title: string;
    icon: string;
    menuItems?: MenuItem[];
}

export const ContextMenu: React.FC<ContextMenuProps> = ({ title: initialTitle, icon, menuItems }) => {
    const [title, setTitle] = useState(initialTitle);

    const onSelected = (index: number) => {
        const menuItem = menuItems[index];
        menuItem.onSelected();
        setTitle(menuItem.name);
    };

    const list = (
        <List>
            {menuItems.map((item, index) => (
                <ListItem key={item.name} onClick={() => onSelected(index)} style={{ cursor: "pointer" }}>
                    {item.icon && (
                        <ListItemIcon>
                            <Icon>{item.icon}</Icon>
                        </ListItemIcon>
                    )}
                    <ListItemText primary={item.name} />
                </ListItem>
            ))}
        </List>
    );

    return (
        <DropDownMenu icon={icon} title={title}>
            {list}
        </DropDownMenu>
    );
};
