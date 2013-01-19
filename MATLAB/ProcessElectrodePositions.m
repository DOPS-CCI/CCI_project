function [ R, S ] = ProcessElectrodePositions( x, y, z, w )
%ProcessElectrodePositions
%   Calculates two matrices R and S that are used to generate
%   an interpolation function for the surface electrode array
%   described by x, y, and z. w is the loading value, usually
%   1.0 (Nunez et al suggest values between 0.5 and 3cm).
%OUTPUTS
%   R 10 x n matrix
%   S n x n matrix where n is the number of electrodes
n=size(x,2);
w2=w^2;
diag=w2^2*log(w2);
k=zeros(n,n);
for i=1:n
    k(i,i)=diag;
    for j=i+1:n
        d=(x(i)-x(j))^2+(y(i)-y(j))^2+(z(i)-z(j))^2+w2;
        k(i,j)=d^2*log(d);
        k(j,i)=k(i,j);
    end;
end;
e=zeros(n,10);
for i=1:n
    e(i,1)=1;
    e(i,2)=x(i);
    e(i,3)=y(i);
    e(i,4)=x(i)^2;
    e(i,5)=x(i)*y(i);
    e(i,6)=y(i)^2;
    e(i,7)=z(i);
    e(i,8)=x(i)*z(i);
    e(i,9)=y(i)*z(i);
    e(i,10)=z(i)^2;
end;
a=(e'/k)*e; %10 x 10 matrix
R=(a\e')/k; %10 x n matrix
S=k\(eye(n)-e*R); %n x n matrix
end