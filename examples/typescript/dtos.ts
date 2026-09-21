export interface User {
    name: string;
    birthDate: string;
}

export interface Talk {
    author: string;
    text: string;
    reply: boolean;
}

export interface UserAstrology extends User {
    weekDay: string;
    sign: string;
}
